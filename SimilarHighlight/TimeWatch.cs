using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimilarHighlight
{
    public static class TimeWatch
    {
        private static Stopwatch watch = new Stopwatch();

        public static void Init()
        {
            watch.Reset();
        }

        public static void Start() {

            if (watch.IsRunning)
            {
                watch.Restart();
            }
            else {
                watch.Start();
            }
        }

        public static void Stop(string strObjName = "") {
            if (watch.IsRunning) {
                watch.Stop();

                Debug.WriteLine(strObjName + "  (total cost " + (watch.ElapsedMilliseconds).ToString() + " seconds)");
            }
        }
    }
}
