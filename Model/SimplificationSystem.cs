using System;
using System.Text;
using System.Collections.Generic;

namespace Model
{
    public class SimplificationSystem
    {
        private string name;
        public string Name
        {
            get { return name; }
        }

        private List<SimplificationRule> rules = null;
        public List<SimplificationRule> Rules
        {
            get { return rules; }
        }

        public string Simplify(string text)
        {
            foreach (SimplificationRule rule in rules)
            {
                text = text.Replace(rule.Text, rule.Replacement);
            }
            return text;
        }

        public SimplificationSystem(string name)
        {
            this.name = name;
            this.rules = new List<SimplificationRule>();
        }
    }
}
