using Promises;
using System;
using System.Timers;

namespace Demo
{
    class Program
    {
        static Promise<string> AskMomForPhone(string phoneIWant, int currentBudget)
        {
            return new Promise<string>((resolve, reject) => {
                if (currentBudget < 600)
                {
                    throw new Exception("Sorry, honey.  Mom is broke.");
                }

                if (currentBudget > 1000)
                {
                    resolve(phoneIWant);
                }

                Timer timer = new Timer(5000);
                timer.Elapsed += (sender, ev) =>
                {
                    var rand = new Random();
                    if (rand.NextDouble() >= 0.5)
                    {
                        resolve(phoneIWant);
                    }
                    else
                    {
                        reject(new Exception("You didn't keep your grades up, no phone for you!"));
                    }

                    timer.Dispose();
                };
                timer.AutoReset = false;
                timer.Enabled = true;
            });
        }


        static void Main(string[] args)
        {
            AskMomForPhone("Samsung", 200)
                .Then(
                    value => Console.WriteLine($"Check out my brand-new {value}!"),
                    ex => Console.WriteLine(ex.Message)
                );

            Console.WriteLine("Press any key to quit...");
            Console.ReadKey();
        }
    }
}
