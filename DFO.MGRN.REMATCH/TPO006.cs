using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Agresso.Interface.TopGenExtension;
using Agresso.Interface.CommonExtension;
using System.Data;
using System.Drawing;



namespace DFO.MGRN.REMATCH
{
    [TopGen("TPO006", "*", "*", "DFO.MGRN.REMATCH - TPO0060")]
    public class TPO006 : IProjectTopGen
    {


        IForm _form;


        public void Initialize(IForm form)
        {
            _form = form;
            _form.OnSaving += _form_OnSaving;
            _form.OnOpenedRow += _form_OnOpenedRow;
            _form.OnSaved += _form_OnSaved;
            _form.OnValidatingField += _form_OnValidatingField;
           
        }

        private void _form_OnSaved(object sender, SaveEventArgs e)
        {

            if (_form.Parameters.Contains("voucher_no"))
            {
                string orderId = _form.Parameters["order_id"].ToString();
                string voucherNo = _form.Parameters["voucher_no"].ToString();

                bool hasDiscrAccount = ChkDiscrAccount(voucherNo);
                if (hasDiscrAccount)
                {
                   // DeleteFromAcrtrans(orderId, voucherNo);
                   // UpdateAlgdelivery(voucherNo);
                   // DeleteApoarrord(voucherNo);
                   // InsertApoinvoiceImport(orderId, voucherNo);
                   // _form.Close();
                    

                }
            }
        }

        private void _form_OnOpenedRow(object sender, OpenRowEventArgs e)
        {
            _form.GetSection("GLAnalysis").IsReadOnly = false;

            _form.GetField("GLAnalysis", "account").SetColumnToReadOnly(e.Row.Table.Rows.IndexOf(e.Row), true);
            _form.GetField("GLAnalysis", "dim_1").SetColumnToReadOnly(e.Row.Table.Rows.IndexOf(e.Row), true);
            _form.GetField("GLAnalysis", "dim_2").SetColumnToReadOnly(e.Row.Table.Rows.IndexOf(e.Row), true);
            _form.GetField("GLAnalysis", "dim_3").SetColumnToReadOnly(e.Row.Table.Rows.IndexOf(e.Row), true);
            _form.GetField("GLAnalysis", "dim_4").SetColumnToReadOnly(e.Row.Table.Rows.IndexOf(e.Row), true);
            _form.GetField("GLAnalysis", "dim_5").SetColumnToReadOnly(e.Row.Table.Rows.IndexOf(e.Row), true);
            _form.GetField("GLAnalysis", "dim_6").SetColumnToReadOnly(e.Row.Table.Rows.IndexOf(e.Row), true);
            _form.GetField("GLAnalysis", "dim_7").SetColumnToReadOnly(e.Row.Table.Rows.IndexOf(e.Row), true);
            _form.GetField("GLAnalysis", "tax_system").SetColumnToReadOnly(e.Row.Table.Rows.IndexOf(e.Row), true);
            _form.GetField("GLAnalysis", "percentage").SetColumnToReadOnly(e.Row.Table.Rows.IndexOf(e.Row), true);
            _form.GetField("GLAnalysis", "amount").IsHidden = true;

            _form.Data.Tables["algdelivery"].AcceptChanges();
        }

        private void _form_OnValidatingField(object sender, ValidateFieldEventArgs e)
        {

            IField artdescr = _form.GetField("order_details", "received_val");
            int rowNr = e.Row.Table.Rows.IndexOf(e.Row);
            IDecorator formDecorator = _form.Decorator;
            IDecoration cellColor   
                = formDecorator.GetCellDecoration(artdescr, rowNr);

            cellColor.BackgroundColour = Color.Aqua;
            

           // artDescrDecoration.ReadOnly = true;
            cellColor.Apply();

            //if (e.TableName == "algdelivery")
            //{
            //    if (e.FieldName == "received_val")
            //    {
            //        e.Row["arr_val"] = e.Row["received_val"].ToString();

            //        foreach (DataRow row in _form.Data.Tables["apodetail"].Rows)
            //        {
            //            if (e.Row["line_no"].ToString() == row["line_no"].ToString())
            //            {
            //                row["arr_val"] = e.Row["arr_val"].ToString();
            //            }
            //        }
            //    }
            //    if (e.FieldName == "rev_price")
            //    {
            //        e.Row["rev_price"] = e.Row["rev_price"].ToString();
            //        foreach (DataRow row in _form.Data.Tables["apodetail"].Rows)
            //        {
            //            if (e.Row["line_no"].ToString() == row["line_no"].ToString())
            //            {
            //                row["arr_amount"] = (double.Parse(e.Row["arr_val"].ToString()) * double.Parse(e.Row["rev_price"].ToString()));
            //                //(double.Parse(row["rev_price"].ToString()) * double.Parse(row["received_val"].ToString()))
            //            }
            //        }
            //    }
            //}
            if (e.TableName == "apovitransdetail")
            {
                if (e.FieldName == "tax_code")
                {

                    string taxCode = e.Row["tax_code"].ToString();
                    string orderId = e.Row["order_id"].ToString();
                    string lineNo = e.Row["line_no"].ToString();

                    IStatement sql = CurrentContext.Database.CreateStatement();
                    sql.Assign($" Update apodetail set tax_code = '{taxCode}' where order_id = {orderId} and line_no = {lineNo} and client = @client ");
                    sql["client"] = _form.Client;

                    CurrentContext.Database.Execute(sql);

                    CurrentContext.Message.Display(MessageDisplayType.Confirmation, "Mva kode oppdater på innkjøpsordren!");

                   // e.Row["tax_code"] = e.Row["tax_code"].ToString();
                    foreach (DataRow row in _form.Data.Tables["algdelivery"].Rows)
                    {
                        if (e.Row["line_no"].ToString() == row["line_no"].ToString())
                        {
                            row["tax_code"] = e.Row["tax_code"].ToString();
                        }
                    }
                }
            }

        }

        private void _form_OnSaving(object sender, SaveEventArgs e)
        {
            if (_form.Parameters.Contains("voucher_no"))
            {
                if (GetAcrtransValue())
                {
                    e.Cancel = true;
                    CurrentContext.Message.Display(MessageDisplayType.Error, "Varemotakk balanserer ikke med fakturaen");

                } 
            }
        }

        private bool GetAcrtransValue ()
        {
            DataTable dt = new DataTable();

            IStatement sql = CurrentContext.Database.CreateStatement();
            sql.Assign("    SELECT sum (amount) as amount from acrtrans  ");
            sql.Append($"   WHERE client = @client and voucher_no = @voucherNo and trans_type = 'GL' ");
            sql["client"] = _form.Client;
            sql["voucherNo"] = _form.Parameters["voucher_no"].ToString();

            CurrentContext.Database.Read(sql, dt);

            double grnAmount = 0;
            //double grnQuantity = 0;

            DataTable algdelivery = _form.Data.Tables["algdelivery"];
            foreach (DataRow row in algdelivery.Rows)
            {

                grnAmount += (double.Parse(row["rev_price"].ToString()) * double.Parse(row["received_val"].ToString())); 
                //grnQuantity += double.Parse(row["received_val"].ToString());

            }
            double balance = double.Parse(dt.Rows[0]["amount"].ToString());
           
            double grnTot = grnAmount;

            double diff = Math.Abs(balance - grnTot);

            if (diff > 0)
                return true;

            else return false;

           
              

        }
        
        private bool ChkDiscrAccount(string voucherNo)
        {
            string client = _form.Client;

            // EI DISCR ACCOUNT PARAM
            string isEiDiscr = "";
            CurrentContext.Session.GetSystemParameter("EI_DISCR_ACCOUNT", client, out isEiDiscr);

            DataTable dt = new DataTable();
            IStatement sql = CurrentContext.Database.CreateStatement();

            sql.Assign(" SELECT account from acrtrans ");
            sql.Append($" WHERE client = @client and voucher_no = '{voucherNo}' and account = '{isEiDiscr}'");
            sql["client"] = _form.Client;

            CurrentContext.Database.Read(sql, dt);
            if (dt.Rows.Count > 0)
                return true;

            return false;

        }
    }
}