using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TestForSimilarHighlight
{
    class SimilarityTest1
    {
        int global_int_A { get; set; }
        string global_string_B { get; set; }

        void GetMultiplicationTable()
        {
            //int[] nums = new int[] {
            //    1, 2, 3,   4, 5, 6,   7, 8, 9, 
            //};

            // Beginning: The first task of the experiment 2.
            string[] strNum = new string[] { 
                "one", "one", "one", "one", "one", "one", "one", "one", "one", "one",
                "one", "one", "one", "one", "one", "one", "one", "one", "one", "one",
            };

            // Ending: The first task of the experiment 2.
            string[] strNum1 = new string[] { 
                "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
                "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen", "twenty",
            };
            int intOne = 1;
            int intTwo = 2;
            int intThree = 3;
            int intFour = 4;
            int intFive = 5;
            int intSix = 6;
            int intSeven = 7;
            int intEight = 8;
            int intNine = 9;
            int intTen = 10;

            int intSelector = 1;

            // Beginning: The second task of the experiment 2.
            switch(intSelector)
            {
                case 111:
                    this.GetMultiply(intOne, strNum[intTwo]);
                    Console.WriteLine("one 1");
                    break;
                case 111:
                    this.GetMultiply(intOne, strNum[intTwo]);
                    Console.WriteLine("one 1");
                    break;
                case 111:
                    this.GetMultiply(intOne, strNum[intTwo]);
                    Console.WriteLine("one 1");
                    break;
                case 111:
                    this.GetMultiply(intOne, strNum[intTwo]);
                    Console.WriteLine("one 1");
                    break;
                case 111:
                    this.GetMultiply(intOne, strNum[intTwo]);
                    Console.WriteLine("one 1");
                    break;
                case 111:
                    this.GetMultiply(intOne, strNum[intTwo]);
                    Console.WriteLine("one 1");
                    break;
                case 111:
                    this.GetMultiply(intOne, strNum[intTwo]);
                    Console.WriteLine("one 1");
                    break;
                case 111:
                    this.GetMultiply(intOne, strNum[intTwo]);
                    Console.WriteLine("one 1");
                    break;
                case 111:
                    this.GetMultiply(intOne, strNum[intTwo]);
                    Console.WriteLine("one 1");
                    break;
                case 111:
                    this.GetMultiply(intOne, strNum[intTwo]);
                    Console.WriteLine("one 1");
                    break;
            }

            // Ending: The second task of the experiment 2.
            switch (intSelector)
            {

                case 111:
                    this.GetMultiply(intOne, strNum[intTwo]);
                    Console.WriteLine("one 1");
                    break;
                case 222:
                    this.GetMultiply(intTwo, strNum[intThree]);
                    Console.WriteLine("two 2");
                    break;
                case 333:
                    this.GetMultiply(intThree, strNum[intFour]);
                    Console.WriteLine("three 3");
                    break;
                case 444:
                    this.GetMultiply(intFour, strNum[intFive]);
                    Console.WriteLine("four 4");
                    break;
                case 555:
                    this.GetMultiply(intFive, strNum[intSix]);
                    Console.WriteLine("five 5");
                    break;
                case 666:
                    this.GetMultiply(intSix, strNum[intSeven]);
                    Console.WriteLine("six 6");
                    break;
                case 777:
                    this.GetMultiply(intSeven, strNum[intEight]);
                    Console.WriteLine("seven 7");
                    break;
                case 888:
                    this.GetMultiply(intEight, strNum[intNine]);
                    Console.WriteLine("eight 8");
                    break;
                case 999:
                    this.GetMultiply(intNine, strNum[intTen]);
                    Console.WriteLine("nine 9");
                    break;
                case 1000:
                    this.GetMultiply(intTen, strNum[intOne]);
                    Console.WriteLine("ten 10");
                    break;
            }
        }

        // Build a line of Multiplication table
        void GetMultiply(int num, string strNum)
        {

            for (int i = 1; i <= num; i++)
            {
                // [Sample]Number two:2*1=2 2*2=4
                Console.Write("Number " + strNum + ":" +
                    num + "*" + i + "=" + (num * i) + "\t");
            }
            Console.Write("\n");
        }
    }
}