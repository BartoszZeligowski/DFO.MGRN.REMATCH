using Agresso.Interface.CommonExtension;
using Agresso.Interface.TopGenExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using ACT.Common.Data;

namespace DFO
{
    public class EventManager
    {
        // Variabel for å holde på fakturadato
        bool DEBUG = false;

        string _dispNo;
        string _contractno;
        string _reasoncode;
        string _reasondescr;
        string _sequenceNo;

        string _voucherNo;
        string _orderID;
        string _arrDate;

        bool _mainParam;

        IForm _form;

        public EventManager(IForm form)
        {
            _form = form;
        }

        public void _form_OnValidatedField(object sender, ValidateFieldEventArgs e)
        {

        }
    }    
}
