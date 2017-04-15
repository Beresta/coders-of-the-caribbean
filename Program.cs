using System;

namespace coders_of_the_caribbean_referee_dotnet
{
    class Program
    {

        static void Main(string[] args)
        {
            var referee = new Referee(Console.OpenStandardInput(), Console.OpenStandardOutput(), Console.OpenStandardError());
        }

    }
}
