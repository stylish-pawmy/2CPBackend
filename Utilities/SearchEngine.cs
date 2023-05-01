namespace _2cpbackend.Utilities;

//Lucine search engine library references
using Lucene.Net;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Store;
using Lucene.Net.Search;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Util;

using Microsoft.EntityFrameworkCore;

using _2cpbackend.Models;
using _2cpbackend.Data;

public class SearchEngine
{
    public readonly LuceneVersion Version = LuceneVersion.LUCENE_48;
    public IndexWriter Writer { get; } = null!;

    public SearchEngine()
    {
        //Initializing the indexWriter
        var analyzer = new StandardAnalyzer(this.Version);
        var directory = FSDirectory.Open("Data/index");
        var indexConfig = new IndexWriterConfig(this.Version, analyzer);
        
        this.Writer = new IndexWriter(directory, indexConfig);
    }

    public void AddToIndex(Event source)
    {
        var doc = new Document
        {
            new StringField("Id", source.Id.ToString(), Field.Store.YES),
            new StringField("Title", source.Title, Field.Store.YES),
            new StringField("Description", source.Description, Field.Store.YES),
            new StringField("OrganizerUserName", source.Organizer.UserName, Field.Store.YES),
            new StringField("OrganizerFullName", $"{source.Organizer.FirstName} {source.Organizer.LastName}", Field.Store.YES),
        };

        this.Writer.AddDocument(doc);
        this.Writer.Flush(false, false);
    }

    public void RemoveFromIndex(Event source)
    {
        var term = new Term("Id", source.Id.ToString());
        this.Writer.DeleteDocuments(term);
        this.Writer.Flush(true, true);
    }

    public IEnumerable<string> SearchEvent(string query, int amount)
    {
        //Query construction
        var phrase = new MultiPhraseQuery
        {
            new Term("Title", query),
            new Term("Description", query),
            new Term("OrganizerUserName", query),
            new Term("OrganizerFullName", query),
            new Term("Category", query)
        };

        using var reader = Writer.GetReader(true);
        var searcher = new IndexSearcher(reader);
        var hits = searcher.Search(phrase, amount).ScoreDocs;

        var results = new List<string>();

        foreach(var hit in hits)
        {
            var foundEvent = searcher.Doc(hit.Doc);
            results.Add(foundEvent.Get("Id"));
        }

        return results;
    }
}