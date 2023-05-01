namespace _2cpbackend.Utilities;

//Lucine search engine library references
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Store;
using Lucene.Net.Search;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Util;

using _2cpbackend.Models;

public class SearchEngine : ISearchEngine
{
    public readonly LuceneVersion Version = LuceneVersion.LUCENE_48;
    private IndexWriter Writer { get; set; } = null!;

    public void GetWriter()
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
        this.Writer.Dispose();
    }

    public void RemoveFromIndex(Event source)
    {
        var term = new Term("Id", source.Id.ToString());
        this.Writer.DeleteDocuments(term);
        this.Writer.Flush(true, true);
        this.Writer.Dispose();
    }

    public IEnumerable<string> SearchEvent(string query, int amount)
    {
        //Query construction
            var title_query = new TermQuery(new Term("Title", query));
            var description_query = new TermQuery(new Term("Description", query));
            var category_query = new TermQuery(new Term("Category", query));
            var organizer_name_query = new TermQuery(new Term("OrganizerUserName", query));
            var organizer_fullname_query = new TermQuery(new Term("OrganizerFullName", query));

        var finalQuery = new BooleanQuery()
        {
            new BooleanClause(title_query, Occur.SHOULD),
            new BooleanClause(description_query, Occur.SHOULD),
            new BooleanClause(category_query, Occur.SHOULD),
            new BooleanClause(organizer_name_query, Occur.SHOULD),
            new BooleanClause(organizer_fullname_query, Occur.SHOULD)
        };

        Console.WriteLine(finalQuery.ToString());

        using var reader = this.Writer.GetReader(true);
        var searcher = new IndexSearcher(reader);
        var hits = searcher.Search(finalQuery, amount).ScoreDocs;

        var results = new List<string>();

        foreach(var hit in hits)
        {
            var foundEvent = searcher.Doc(hit.Doc);
            results.Add(foundEvent.Get("Id"));
        }

        return results;
    }

    public void DisposeWriter()
    {
        this.Writer.Dispose(); 
    }
}