using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MKOTHDiscordBot.Properties;
using LiteDB;
using Microsoft.Extensions.Options;

namespace MKOTHDiscordBot.Services
{
    public class IssueTracker: IDisposable
    {
        private readonly LiteDatabase database;
        private readonly LiteCollection<Issue> collection;

        public IssueTracker(IOptions<AppSettings> appSettings)
        {
            database = new LiteDatabase(appSettings.Value.ConnectionStrings.ApplicationDb);
            collection = database.GetCollection<Issue>();
        }

        public void CreateIssue(string title, string content)
        {
            collection.Insert(new Issue { Title = title, Content = content });
        }

        public bool UpdateIssue(int id, string title, string content)
        {
            var issue = collection.FindById(id);
            if (issue == null)
            {
                return false;
            }
            else
            {
                issue.Title = title;
                issue.Content = content;
                collection.Update(id, issue);
                return true;
            }
        }

        public IEnumerable<Issue> GetIssues()
        {
            return collection.FindAll();
        }

        public bool DeleteId(int id)
        {
            return collection.Delete(id);
        }

        public void Dispose()
        {
            database.Dispose();
        }
    }

    public class Issue
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;
        public string Title { get; set; }
        public string Content { get; set; }
    }
}
