using System;
using System.Collections.Generic;
using OrigoDB.Core;
using ProtoBuf;

namespace Modules.ProtoBuf.Test.Domain
{

    [ProtoContract(ImplicitFields=ImplicitFields.AllFields, AsReferenceDefault = true)]
    public class SpecialTodoItem : TodoItem
    {
        public string SpecialValue { get; set; }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields, AsReferenceDefault = true)]
    [ProtoInclude(100, typeof(SpecialTodoItem))]
    public class TodoItem
    {
        public readonly Guid Id;
        public string Title { get; set; }
        public DateTime? Due;
        public DateTime? Completed;

        public TodoItem()
        {
            Id = Guid.NewGuid();
            Title = "No name";
        }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields, AsReferenceDefault = true, SkipConstructor = true)]
    public class Category
    {
        public string Name { get; set; }
        public List<TodoItem> Items { get; set; }

        public Category(string name)
        {
            Name = name;
            Items = new List<TodoItem>();
        }
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    public class TodoModel : Model
    {
        public Dictionary<Guid, TodoItem> Items 
        {
            get;
            private set;
        }

        public Dictionary<string, Category> Categories
        {
            get;
            private set;
        }

        public TodoModel()
        {
            Items = new Dictionary<Guid, TodoItem>();
            Categories = new Dictionary<string, Category>(StringComparer.InvariantCultureIgnoreCase);
        }

        public Guid AddItem(string title)
        {
            var item = new TodoItem{Title = title};
            Items.Add(item.Id, item);
            return item.Id;
        }

        public Guid AddSpecialItem(string title)
        {
            var item = new SpecialTodoItem { Title = title };
            Items.Add(item.Id, item);
            return item.Id;
        }

        public void SetCategories(Guid itemId, params string[] categoryNames)
        {
            TodoItem item;
            if (Items.TryGetValue(itemId, out item))
            {
                foreach (var categoryName in categoryNames)
                {
                    if (!Categories.ContainsKey(categoryName))
                    {
                        Categories.Add(categoryName, new Category(categoryName));
                    }
                    Categories[categoryName].Items.Add(item);
                }
            }
        }

        [ProtoAfterDeserialization]
        private void FixRefsAfterDeserialization()
        {
            foreach (var category in Categories.Values)
            {
                for (int i = 0; i < category.Items.Count; i++)
                {
                    category.Items[i] = Items[category.Items[i].Id];
                }
            }
        }
    }
}