namespace Jinak.Utility.Help
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class HelpPageAttribute : Attribute
    {
        public string Name;
        public string Description;
        public string[] Aliases = new string[] { };

        public string[] Names
        {
            get
            {
                List<string> leest = new List<string>();
                leest.Add(Name);
                leest.AddRange(Aliases);
                return leest.ToArray();
            }
        }

        public HelpPageAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }

        public HelpPageAttribute(string name, string description, params string[] aliases)
        {
            Name = name;
            Description = description;
            Aliases = aliases;
        }
    }
}