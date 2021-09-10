using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Agresso.Interface.TopGenExtension;
using Agresso.Interface.CommonExtension;
using System.Data;

namespace DFO.MGRN.REMATCH
{
    [TopGen("TPO060", "*", "*", "DFO.MGRN.REMATCH - TPO0060")]
    public class TPO060 : IProjectTopGen
    {


        IForm _form;


        public void Initialize(IForm form)
        {
            _form = form;
            _form.OnCallingAction += _form_OnCallingAction;
            _form.OnCalledAction += _form_OnCalledAction;
            _form.OnOpenedRow += _form_OnOpenedRow;
            
                
        }


        private void _form_OnOpenedRow(object sender, OpenRowEventArgs e)
        {
           
            DataTable dt = _form.Data.Tables["receipt_proposal"];
            bool contains = dt.AsEnumerable().Any(row =>  row.Field<bool>("receipted")== true);
            if (contains)
            {
                

                for (int i = dt.Rows.Count - 1; i >= 0; i--)
                {
                    DataRow row = dt.Rows[i];
                    if (row["receipted"].ToString() == "False")
                    {
                        row.Delete();
                    }
                }

                _form.Data.Tables["receipt_proposal"].AcceptChanges();
            }
        }
        private void _form_OnCallingAction(object sender, ActionEventArgs e)
        {
            //OpenReceiptAction();

            string voucherNo = _form.Data.Tables["acrtrans"].Rows[0]["voucher_no"].ToString();
            string orderId = _form.Data.Tables["apoheader"].Rows[0]["order_id"].ToString();

            bool grnExists = GetGrnValue(orderId, voucherNo);
            bool hasDiscrAccount = ChkDiscrAccount(voucherNo);

            //GetInvoice(voucherNo);

            if (e.Action == "TASKM:AP")
            {
                if (hasDiscrAccount)
                {
                    if (_form.Data.Tables["receipt_proposal"].Rows.Count > 0)
                    {
                        if (_form.Data.Tables["receipt_proposal"].Rows[0]["arrive_id"].ToString() == "0")
                        {


                            CurrentContext.Message.Display(MessageDisplayType.Warning, "Ta varemottak!");
                            OpenReceiptAction();
                            // _form.Open($"topgen:menu_id=PO286&order_id={orderId}&voucher_no={voucherNo}");
                            e.Cancel = true;




                        }
                        else
                        {
                            AddRematchParameter(voucherNo, orderId);

                        }
                    }
                    else
                        AddRematchParameter(voucherNo, orderId);


                }

            }


        }

        private void _form_OnCalledAction(object sender, ActionEventArgs e)
        {
            e.Cancel = true;

            if (_form.Parameters.Contains("rematchVoucher"))
            {
                string voucherNo = _form.Parameters["rematchVoucher"].ToString();
                string orderId = _form.Parameters["rematchOrderid"].ToString();

                DeleteFromAcrtrans(orderId, voucherNo);
                UpdateAlgdelivery(voucherNo);
                UpdateApodetail(orderId);
                DeleteApoarrord(voucherNo);
                InsertApoinvoiceImport(orderId, voucherNo);
            }
        }

        private void OpenReceiptAction()
        {
            //_form.Open("topgen:menu_id=PO286&client=UB&order_id=600005303&voucher_no=900012251");

            try
            {
                IMenuItem receiptBtn = _form.Menu.Toolbar.GetByName("receipt");

                IActionCommand cmd = _form.Commands.CreateActionCommand(receiptBtn);
                //var cmd = _form.Commands.CreateActionCommand("receipt");
                cmd.Execute();
            }
            catch (Exception ex)
            {

                CurrentContext.Message.Display($"Ta deg en bolle: {ex.Message}");
            }
        }

        private bool ChkDiscrAccount( string voucherNo)
        {
            string client = _form.Client;

            // EI DISCR ACCOUNT PARAM
            string isEiDiscr = "";
            CurrentContext.Session.GetSystemParameter("EI_DISCR_ACCOUNT", client, out isEiDiscr);
            
            DataTable dt = new DataTable();
            IStatement sql = CurrentContext.Database.CreateStatement ();

            sql.Assign(" SELECT account from acrtrans ");
            sql.Append($" WHERE client = @client and voucher_no = '{voucherNo}' and account = '{isEiDiscr}'");
            sql["client"] = _form.Client;

            CurrentContext.Database.Read(sql,dt);
            if (dt.Rows.Count > 0)
                return true;

            return false;

        }

        private void AddRematchParameter(string voucherNo, string orderId)
        {
            if (!_form.Parameters.Contains("rematchVoucher"))
            {
                _form.Parameters.Add("rematchVoucher", voucherNo);
                _form.Parameters.Add("rematchOrderid", orderId);

            }
            else
            {
                _form.Parameters["rematchVoucher"] = voucherNo;
                _form.Parameters["rematchOrderid"] = voucherNo;

            }
        }

        private bool GetGrnValue(string orderId, string voucherNo)
        {
            
            DataTable dt = new DataTable();
            IStatement sql = CurrentContext.Database.CreateStatement();
            sql.Assign("    SELECT * from apoarrord ");
            sql.Append($"   WHERE client = @client and arrive_id = '{voucherNo}' and arrive_id2 != 0 and order_id = '{orderId}' ");
            sql["client"] = _form.Client;
            

            CurrentContext.Database.Read(sql, dt);

            if (dt.Rows.Count > 0)
                return true;

            return false;
        }

        private void DeleteFromAcrtrans(string orderId, string voucherNo)
        {
          
            IStatement sql = CurrentContext.Database.CreateStatement();
            sql.Assign(" DELETE from acrtrans");
            sql.Append($" WHERE client = @client and voucher_no = '{voucherNo}' and order_id = '{orderId}' ");
            sql.Append("  AND trans_type != 'AP'  ");
            sql["client"] = _form.Client;
        

            CurrentContext.Database.Execute(sql);

        }

        private void InsertApoinvoiceImport(string orderId, string voucherNo)
        {
            IStatement sql = CurrentContext.Database.CreateStatement();
            sql.Assign("INSERT INTO apoinvoiceimport (b_flag, client, tot_tax_cur_amt, trans_date, voucher_no) ");
            sql.Append($" SELECT '1', 'UB', b.sum_tax_cur_amt, voucher_date, '{voucherNo}' ");
            sql.Append(" FROM acrtrans a ");
            sql.Append(" INNER JOIN udv_iin_taxamount b on a.client = b.client and a.voucher_no = b.voucher_no ");
            sql.Append($" WHERE a.client = @client and a.trans_type = 'AP' and a.voucher_no = '{voucherNo}' ");
            sql.Append(" AND NOT EXISTS ");
            sql.Append($" (SELECT 1 From apoinvoiceimport Where client = @client AND voucher_no = '{voucherNo}') ");
            sql["client"] = _form.Client;

            CurrentContext.Database.Execute(sql);
        }

        private void UpdateAlgdelivery(string voucherNo)
        {
            IStatement sql = CurrentContext.Database.CreateStatement();
            sql.Append("UPDATE a SET a.arr_amount = 0, a.arr_val = 0 ");
            sql.Append("FROM algdelivery a ");
            sql.Append("INNER JOIN apoarrord b ON a.client =b.client AND a.order_id=b.order_id AND a.line_no =b.line_no AND a.arrive_id = b.arrive_id2 ");
            sql.Append("INNER JOIN apodetail d ON a.client = d.client AND a.order_id = d.order_id AND a.line_no = d.line_no ");
            sql.Append($"WHERE a.client = @client AND b.arrive_id = '{voucherNo}' ");

            sql["client"] = _form.Client;

            CurrentContext.Database.Execute(sql);

        }

        private void DeleteApoarrord(string voucherNo)
        {
            IStatement sql = CurrentContext.Database.CreateStatement();
            sql.Assign(" DELETE FROM apoarrord ");
            sql.Append($"WHERE arrive_id = '{voucherNo}' AND client = @client ");
            sql["client"] = _form.Client;
            
            CurrentContext.Database.Execute(sql);

        }

        private void UpdateApodetail(string orderId)
        {
            IStatement sql = CurrentContext.Database.CreateStatement();
            sql.Append("UPDATE a SET a.arr_amount = 0, a.arr_val = 0 ");
            sql.Append("FROM apodetail a ");
            sql.Append($"WHERE a.client = @client AND a.order_id = '{orderId}' ");

            sql["client"] = _form.Client;

            CurrentContext.Database.Execute(sql);
        }

        private void GetInvoice(string voucherNo)
        {
            IStatement sql = CurrentContext.Database.CreateStatement();
            sql.Assign("DATABASE SELECT DISTINCT a.client, c.voucher_no, a.order_id, c.arrive_id, a.line_no, ");
            sql.Append("MAX(a.arrive_id2) AS max_arr_arrive_id, MAX(x.max_arriveId) AS max_del_arrive_id ");
            sql.Append("FROM apoarrord a ");
            sql.Append("JOIN algdelivery b ON a.client =b.client AND a.order_id=b.order_id AND a.line_no =b.line_no AND b.apar_type ='P' AND b.wf_state IN ('N','T') ");
            sql.Append("JOIN acrtrans c ON a.client = c.client AND a.order_id = c.order_id AND a.arrive_id = c.voucher_no ");
            sql.Append("JOIN acrtransmap d ON c.client =d.client AND c.voucher_no =d.voucher_no AND c.sequence_no = d.sequence_no ");
            sql.Append("JOIN awftask e ON e.oid =d.oid AND e.active ='1' AND e.wf_status ='ACT' ");
            sql.Append("INNER JOIN awfprocfunction f ON e.client = f.client AND e.version_no = f.version_no AND e.node_id = f.node_id AND f.menu_ref='279' "); // Menu ref Saknad varumottagning
            sql.Append("INNER JOIN (select b1.client, b1.order_id, b1.line_no, ISNULL (MAX(b1.arrive_id),0) max_arriveId ");
            sql.Append("    From algdelivery b1 WHERE b1.wf_state IN ('N','T') GROUP BY b1.client, b1.order_id, b1.line_no) x ");
            sql.Append("    ON a.client =x.client and a.order_id=x.order_id and a.line_no =x.line_no ");
            sql.Append(@"WHERE a.client =@client " );
            sql.Append(" ");
            sql.Append("    AND NOT EXISTS ");
            sql.Append("        (Select 1 From apoinvinfo i WHERE b.client = i.client AND b.order_id = i.order_id AND b.line_no = i.line_no AND b.arrive_id = i.arrive_id ");
            sql.Append("        ) ");
            sql.Append("AND a.order_id NOT IN (Select j.order_id From apodetail j WHERE a.client = j.client AND a.order_id = j.order_id AND a.line_no = j.line_no AND j.vow_val = 0) ");
            sql.Append("GROUP BY a.client,c.voucher_no, a.order_id, c.arrive_id, a.line_no ");
            sql.Append("HAVING  MAX(x.max_arriveId) > MAX(a.arrive_id2) ");
            sql.Append("ORDER BY a.client, c.voucher_no, a.line_no ");
            sql["client"] = _form.Client;

           
            DataTable dt = new DataTable("rematchinvoice");
            CurrentContext.Database.Read(sql, dt);
         
        }
    }
}
