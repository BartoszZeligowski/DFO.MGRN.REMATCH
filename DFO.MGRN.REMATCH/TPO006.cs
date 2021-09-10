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
            //_form.OnDataSchemaValidate += _form_OnDataSchemaValidate;
           
        }

        //private void _form_OnDataSchemaValidate(object sender, DataSchemaValidateEventArgs e)
        //{
        //    ISection accountingSec = _form.GetSection("GLAnalysis");

        //    DataTable accountingSecTable = _form.Data.Tables[accountingSec.TableName];

        //    accountingSecTable.Columns.Add("ChangeTax", typeof(string));
        //}

       

        private void _form_OnSaved(object sender, SaveEventArgs e)
        {
            if (!e.Cancel)
            {

                if (_form.Parameters.Contains("voucher_no"))
                {
                    DataTable dtDelivery = _form.Data.Tables["algdelivery"];
                    DataTable dt = _form.Data.Tables["apovitransdetail"];

                    foreach (DataRow row in dt.Rows)
                    {
                        string taxCode = row["tax_code"].ToString();
                        string orderId = row["order_id"].ToString();
                        string lineNo = row["line_no"].ToString();
                        string userId = CurrentContext.Session.UserId.ToString();
                        string voucherNo = _form.Parameters["voucher_no"].ToString();



                        foreach (DataRow rowdelivery in dtDelivery.Rows)
                        {
                            string oldTaxCode = rowdelivery["tax_code"].ToString();
                            string delivLineNo = rowdelivery["line_no"].ToString();


                            if (taxCode != oldTaxCode && lineNo == delivLineNo)
                            {

                                IStatement sql = CurrentContext.Database.CreateStatement();
                                sql.Assign(" Update a ");
                                sql.Append($" SET a.tax_code = '{taxCode}', a.tax_percent = b.vat_pct ");
                                sql.Append(" , a.tax_amount = ROUND(a.amount * (b.vat_pct/100),2) ");
                                sql.Append(" , a.tax_cur_amt = ROUND(a.cur_amount * (b.vat_pct/100),2) ");
                                sql.Append($" , a.last_update = getdate() , a.user_id ='{userId}' ");
                                sql.Append(" FROM apodetail a, agltaxcode b ");
                                sql.Append($" WHERE a.client = b.client and b.tax_code = '{taxCode}' and cast(getdate() as date) between b.date_from and b.date_to ");
                                sql.Append($" AND a.order_id = {orderId} and a.line_no = {lineNo} and a.client = @client ");
                                sql["client"] = _form.Client;

                                CurrentContext.Database.Execute(sql);

                                IStatement sql1 = CurrentContext.Database.CreateStatement();
                                sql1.Assign(" INSERT INTO udt_iin_apochangelog ");
                                sql1.Append(" (client ,voucher_no, order_id,line_no,type,last_update,user_id,old_val,new_val) ");
                                sql1.Append(" Values ");
                                sql1.Append($" (@client, {voucherNo}, {orderId}, {lineNo}, 'MVA', getdate(), '{userId}', '{oldTaxCode}','{taxCode}' ) ");
                                sql1["client"] = _form.Client;

                                CurrentContext.Database.Execute(sql1);


                                CurrentContext.Message.Display(MessageDisplayType.Confirmation, "Mva kode oppdatert på innkjøpsordren!");
                            }
                        }

                    }
                }

                CurrentContext.Message.Display(MessageDisplayType.Confirmation, "Mva kode oppdater på innkjøpsordren!");
            }
        }

        private void _form_OnOpenedRow(object sender, OpenRowEventArgs e)
        {
           // if (e.Row.Table.TableName == "apovitransdetail")
            //{
                foreach (DataRow row in _form.Data.Tables["apovitransdetail"].Rows)
                {
                    _form.GetSection("GLAnalysis").IsReadOnly = false;

                    _form.GetField("GLAnalysis", "account").SetColumnToReadOnly(row.Table.Rows.IndexOf(row), true);
                    _form.GetField("GLAnalysis", "act_req_asset_extra_asset_status").SetColumnToReadOnly(row.Table.Rows.IndexOf(row), true);
                    _form.GetField("GLAnalysis", "dim_1").SetColumnToReadOnly(row.Table.Rows.IndexOf(row), true);
                    _form.GetField("GLAnalysis", "dim_2").SetColumnToReadOnly(row.Table.Rows.IndexOf(row), true);
                    _form.GetField("GLAnalysis", "dim_3").SetColumnToReadOnly(row.Table.Rows.IndexOf(row), true);
                    _form.GetField("GLAnalysis", "dim_4").SetColumnToReadOnly(row.Table.Rows.IndexOf(row), true);
                    _form.GetField("GLAnalysis", "dim_5").SetColumnToReadOnly(row.Table.Rows.IndexOf(row), true);
                    _form.GetField("GLAnalysis", "dim_6").SetColumnToReadOnly(row.Table.Rows.IndexOf(row), true);
                    _form.GetField("GLAnalysis", "dim_7").SetColumnToReadOnly(row.Table.Rows.IndexOf(row), true);
                    _form.GetField("GLAnalysis", "tax_system").SetColumnToReadOnly(row.Table.Rows.IndexOf(row), true);
                    _form.GetField("GLAnalysis", "tax_code").SetColumnToReadOnly(row.Table.Rows.IndexOf(row), false);
                    _form.GetField("GLAnalysis", "percentage").SetColumnToReadOnly(row.Table.Rows.IndexOf(row), true);
                    _form.GetField("GLAnalysis", "amount").IsHidden = true;
                }

                //_form.Data.Tables["algdelivery"].AcceptChanges(); 
            //}
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
            sql.Assign("    SELECT sum (reg_amount) as amount from acrtrans  ");
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
            double balance = Math.Round(double.Parse(dt.Rows[0]["amount"].ToString()),2);
           
            double grnTot = Math.Round(grnAmount,2);

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