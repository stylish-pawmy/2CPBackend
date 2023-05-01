namespace _2cpbackend.Utilities;

using Lucene.Net.Index;

using _2cpbackend.Models;


public interface ISearchEngine
{
    void GetWriter();
    void AddToIndex(Event source);
    void RemoveFromIndex(Event source);
    IEnumerable<string> SearchEvent(string query, int amount);
    void DisposeWriter();
}