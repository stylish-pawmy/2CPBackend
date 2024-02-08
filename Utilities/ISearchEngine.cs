namespace Eventi.Server.Utilities;

using Lucene.Net.Index;

using Eventi.Server.Models;


public interface ISearchEngine
{
    void GetWriter();
    void AddToIndex(Event source);
    void RemoveFromIndex(Event source);
    IEnumerable<string> SearchEvent(string query, int amount);
    void DisposeWriter();
}