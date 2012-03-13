using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PasswordEvolution
{
    public class PasswordInfo
    {
        private int accounts;
        private double reward;

        public PasswordInfo(int accounts, double reward)
        {
            this.accounts = accounts;
            this.reward = reward;
        }

        public int Accounts
        {
            set { this.accounts = value; }
            get { return this.accounts; }
        }

        public double Reward
        {
            set { this.reward = value; }
            get { return this.reward; }
        }
 
    }
}
