using Agresso.Interface.TopGenExtension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DFO.MGRN.REMATCH
{
    [TopGen("TPO002", "*", "*", "DFO.MGRN.REMATCH - TPO0060")]

    class TPO080 : IProjectTopGen
    {
        IForm _form;
        public void Initialize(IForm form)
        {
            _form = form;

            _form.OnInitialized += _form_OnInitialized;
        }

        private void _form_OnInitialized(object sender, InitializeEventArgs e)
        {
            _form.Open("topgen:menu_id=PO286&client=UB&order_id=600005303&voucher_no=900012251");
        }
    }
}
