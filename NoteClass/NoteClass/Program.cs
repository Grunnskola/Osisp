using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using TracerDll;

namespace NoteClass
{
    public class Note
    {
        public static string hello = "NeverTalkAgainWhatYouWant";
        public static int[] mas = new int[10];
        string statement = "Right";


        public int[] InsertInMas(int[] n)
        {
            Tracer.StartTrace();
            Random rand = new Random();
            for (int i = 0; i < n.Length; i++)
            {
                n[i] = rand.Next(101);
            }
            Tracer.StopTrace();
            return n;
        }

        public string Covercaet(string s1, int index)
        {
            Tracer.StartTrace();
            s1.Substring(index);
            Thread.Sleep(1000);
            Waiter();
            Tracer.StopTrace();
            return s1;

        }

        public string MakeAStory(string s)
        {
            Tracer.StartTrace();
            string outs = "";
            string dayStr = "FineSunnyDay";
            dayStr.Reverse();
            outs = s + dayStr;
            Tracer.StopTrace();
            return outs;
        }
        public string AdditionString()
        {
            Tracer.StartTrace();
            string s = "Money";
            string result;
            var s2 = Covercaet(statement, 2);
            Tracer.StopTrace();
            return result = s + s2;
        }
        public void OutOnConsole(string s)
        {
            Tracer.StartTrace();
            Console.WriteLine("{0}", s);

            Tracer.StopTrace();
        }

        public static void Waiter()
        {
            Tracer.StartTrace();
            Thread.Sleep(1000);
            Tracer.StopTrace();
        }

        public int[] SortingMas(int[] m)
        {
            Tracer.StartTrace();
            int tmp = 0, i, j;
            Thread.Sleep(1000);
            for (i = 0; i < m.Length - 1; ++i) // i - номер прохода
            {
                for (j = 0; j < m.Length - 1; ++j) // внутренний цикл прохода
                {
                    if (m[j + 1] < m[j])
                    {
                        tmp = m[j + 1];
                        m[j + 1] = m[j];
                        m[j] = tmp;
                    }
                    //Waiter();
                }
            }

            Tracer.StopTrace();
            return m;
        }
        static void Main(string[] args)
        {
            Note nt = new Note();

            var sort = nt.SortingMas(nt.InsertInMas(mas));
            for (int i = 0; i < sort.Length; i++)
            {
                Console.Write("{0}\n", sort[i]);
            }

                Waiter();

                var par = nt.Covercaet(hello, 5);
                nt.OutOnConsole(par);
                var c = nt.MakeAStory(nt.AdditionString()); //Коверкает - вложенность по идее = 2
                nt.OutOnConsole(c); //AdditionalString по идее вложенность = 3?

                Tracer.ParseTracedData();
                Tracer.ShowStringResult();
                Tracer.SaveXmlResult();
                Console.Read();
            
        }
    }
}