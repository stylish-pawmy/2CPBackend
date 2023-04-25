namespace _2cpbackend.Utilities;

using Microsoft.AspNetCore.Mvc.ModelBinding;

public class ModelUtils
{
    public static string GetModelErrors(ModelStateDictionary.ValueEnumerable values)
    {
        string body = string.Empty;
        foreach (ModelStateEntry entry in values)
        {
            foreach(ModelError error in entry.Errors)
            {
                body += error.ErrorMessage + "\n";
            }
        }
        return body;
    }
}