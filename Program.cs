using System;
using System.Collections.Generic;
using System.Text;

namespace coders_of_the_caribbean_engine_dotnet
{
    class Program
    {

        static void Main(string[] args)
        {
            var engine = new Engine(Console.OpenStandardInput(), Console.OpenStandardOutput(), Console.OpenStandardError());
        }

    }
}
