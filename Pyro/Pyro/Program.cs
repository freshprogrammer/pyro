using System;

namespace Snake
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (DrMarioGame game = new DrMarioGame())
            {
                game.Run();
            }
        }
    }
#endif
}

