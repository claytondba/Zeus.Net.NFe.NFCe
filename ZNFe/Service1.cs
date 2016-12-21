using BLL;
using NFe.Classes.Protocolo;
using NFeMCInt;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ZNFe
{
    public partial class Service1 : ServiceBase
    {
        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            nfeTimer.Enabled = true;
        }

        protected override void OnStop()
        {
        }

        private void nfeTimer_Tick(object sender, EventArgs e)
        {
            EnviaNFe();
        }

        void EnviaNFe()
        {
            NFeBLL nBll = new NFeBLL();
            DataTable dt = nBll.GetListNfeToSend();

            if (dt.Rows.Count == 0) return;

            foreach (DataRow dr in dt.Rows)
            {
                string xml = "";
                protNFe protocolo = new Form1().EnviaNFeSilent(dr["ide_nnf"].ToString(), out xml);

                
                nBll.SalvaRetornoNFe(protocolo.infProt.chNFe, protocolo.infProt.dhRecbto, protocolo.infProt.digVal, protocolo.infProt.cStat.ToString(),
                    protocolo.infProt.xMotivo, protocolo.infProt.nProt, protocolo.infProt.nProt, xml, dr["ide_nnf"].ToString());


            }

        }
    }
}
