using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Timers;
using RWCF;


namespace RWS
{
    public partial class RWS : ServiceBase
    {
        ServiceHost host = new ServiceHost(typeof(RService));
        RQueue rq = null;
        Object locker = new Object();
        public RWS()
        {
            InitializeComponent();
            rq = RQueue.getInstance();
            Timer tEQ = new Timer();  
            tEQ.Elapsed += new ElapsedEventHandler(ExecQueue);
            tEQ.Interval = 30000;
            tEQ.Enabled = true;
            
        }

        protected override void OnStart(string[] args)
        {
            host.Open();
        }

        protected override void OnStop()
        {
            host.Close();
        }

        /// <summary>
        /// 执行队列
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        public void ExecQueue(Object source, ElapsedEventArgs e)
        {
            lock (locker)
            {
                while (rq.Count > 0)
                {
                    string strModGUID = rq.Dequeue();
                    RService.GenerDstree(strModGUID);
                }
            }
        }
    }
}
