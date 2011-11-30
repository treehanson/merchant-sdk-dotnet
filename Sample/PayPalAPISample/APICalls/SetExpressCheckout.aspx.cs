using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;

using PayPal.Manager;
using PayPal.PayPalAPIInterfaceService;
using PayPal.PayPalAPIInterfaceService.Model;

namespace PayPalAPISample.APICalls
{
    public partial class SetExpressCheckout : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if(Request.Params.Get("paymentAction") != null )
            {
                String action = Request.Params.Get("paymentAction");
                paymentAction.SelectedValue = Request.Params.Get("paymentAction");                
            }
            UriBuilder uriBuilder = new UriBuilder(Request.Url.ToString());
            uriBuilder.Path = "APICalls/GetExpressCheckoutDetails.aspx";
            returnUrl.Value = uriBuilder.Uri.ToString();
        }

        protected void Submit_Click(object sender, EventArgs e)
        {
            // Create request object
            SetExpressCheckoutRequestType request = new SetExpressCheckoutRequestType();
            populateRequestObject(request);

            // Invoke the API
            SetExpressCheckoutReq wrapper = new SetExpressCheckoutReq();
            wrapper.SetExpressCheckoutRequest = request;
            PayPalAPIInterfaceServiceService service = new PayPalAPIInterfaceServiceService();
            SetExpressCheckoutResponseType setECResponse = service.SetExpressCheckout(wrapper);

            // Check for API return status
            Session["paymentDetails"] = request.SetExpressCheckoutRequestDetails.PaymentDetails;
            setKeyResponseObjects(service, setECResponse);
        }

        private void populateRequestObject(SetExpressCheckoutRequestType request)
        {
            SetExpressCheckoutRequestDetailsType ecDetails = new SetExpressCheckoutRequestDetailsType();
            if(returnUrl.Value != "")
            {
                ecDetails.ReturnURL = returnUrl.Value;
            }
            if(cancelUrl.Value != "")
            {
                ecDetails.CancelURL = cancelUrl.Value;
            }
            if (buyerEmail.Value != "")
            {
                ecDetails.BuyerEmail = buyerEmail.Value;
            }
            if (addressoverride.SelectedIndex != 0)
            {
                ecDetails.AddressOverride = addressoverride.SelectedValue;
            }
            if (noShipping.SelectedIndex != 0)
            {
                ecDetails.NoShipping = noShipping.SelectedValue;
            }
            if (solutionType.SelectedIndex != 0)
            {
                ecDetails.SolutionType = (SolutionTypeType)
                    Enum.Parse(typeof(SolutionTypeType), solutionType.SelectedValue);
            }

            /* Populate payment requestDetails. 
             * SetExpressCheckout allows parallel payments of upto 10 payments. 
             * This samples shows just one payment.
             */
            PaymentDetailsType paymentDetails = new PaymentDetailsType();
            ecDetails.PaymentDetails.Add(paymentDetails);
            double orderTotal = 0.0;
            double itemTotal = 0.0;
            CurrencyCodeType currency = (CurrencyCodeType)
                Enum.Parse(typeof(CurrencyCodeType), currencyCode.SelectedValue);
            if (shippingTotal.Value != "")
            {
                paymentDetails.ShippingTotal = new BasicAmountType(currency, shippingTotal.Value);
                orderTotal += Double.Parse(shippingTotal.Value);
            }
            if (insuranceTotal.Value != "" && !double.Parse(insuranceTotal.Value).Equals(0.0))
            {
                paymentDetails.InsuranceTotal = new BasicAmountType(currency, insuranceTotal.Value);                
                paymentDetails.InsuranceOptionOffered = "true";
                orderTotal += Double.Parse(insuranceTotal.Value);
            }
            if (handlingTotal.Value != "")
            {
                paymentDetails.HandlingTotal = new BasicAmountType(currency, handlingTotal.Value);
                orderTotal += Double.Parse(handlingTotal.Value);
            }
            if (taxTotal.Value != "")
            {
                paymentDetails.TaxTotal = new BasicAmountType(currency, taxTotal.Value);
                orderTotal += Double.Parse(taxTotal.Value);
            }
            if (orderDescription.Value != "")
            {
                paymentDetails.OrderDescription = orderDescription.Value;
            }
            paymentDetails.PaymentAction = (PaymentActionCodeType)
                Enum.Parse(typeof(PaymentActionCodeType), paymentAction.SelectedValue);
            if (shippingName.Value != "" && shippingStreet1.Value != "" 
                && shippingCity.Value != "" && shippingState.Value != "" 
                && shippingCountry.Value != "" && shippingPostalCode.Value != "")
            {
                AddressType shipAddress = new AddressType();
                shipAddress.Name = shippingName.Value;
                shipAddress.Street1 = shippingStreet1.Value;
                shipAddress.Street2 = shippingStreet2.Value;
                shipAddress.CityName = shippingCity.Value;
                shipAddress.StateOrProvince = shippingState.Value;
                shipAddress.Country = (CountryCodeType)
                    Enum.Parse(typeof(CountryCodeType), shippingCountry.Value);
                shipAddress.PostalCode = shippingPostalCode.Value;
                ecDetails.PaymentDetails[0].ShipToAddress = shipAddress;
            }
            
            // Each payment can include requestDetails about multiple items
            // This example shows just one payment item
            if (itemName.Value != null && itemAmount.Value != null && itemQuantity.Value != null)
            {
                PaymentDetailsItemType itemDetails = new PaymentDetailsItemType();
                itemDetails.Name = itemName.Value;
                itemDetails.Amount = new BasicAmountType(currency, itemAmount.Value);
                itemDetails.Quantity = Int32.Parse(itemQuantity.Value);
                itemDetails.ItemCategory = (ItemCategoryType)
                    Enum.Parse(typeof(ItemCategoryType), itemCategory.SelectedValue);
                itemTotal += Double.Parse(itemDetails.Amount.value) * itemDetails.Quantity.Value;
                if (salesTax.Value != "")
                {
                    itemDetails.Tax = new BasicAmountType(currency, salesTax.Value);
                    orderTotal += Double.Parse(salesTax.Value);
                }
                if (itemDescription.Value != "")
                {
                    itemDetails.Description = itemDescription.Value;
                }
                paymentDetails.PaymentDetailsItem.Add(itemDetails);                
            }
            orderTotal += itemTotal;
            paymentDetails.ItemTotal = new BasicAmountType(currency, itemTotal.ToString());
            paymentDetails.OrderTotal = new BasicAmountType(currency, orderTotal.ToString());



            // Set Billing agreement (for Reference transactions & Recurring payments)
            if (billingAgreementText.Value != "")
            {
                BillingCodeType billingCodeType = (BillingCodeType)
                    Enum.Parse(typeof(BillingCodeType), billingType.SelectedValue);
                BillingAgreementDetailsType baType = new BillingAgreementDetailsType(billingCodeType);
                baType.BillingAgreementDescription = billingAgreementText.Value;
                ecDetails.BillingAgreementDetails.Add(baType);
            }


            // Set styling attributes for PayPal page
            if (pageStyle.Value != "")
            {
                ecDetails.PageStyle = pageStyle.Value;
            }
            if (cppheaderimage.Value != "")
            {
                ecDetails.cppheaderimage = cppheaderimage.Value;
            }
            if (cppheaderbordercolor.Value != "")
            {
                ecDetails.cppheaderbordercolor = cppheaderbordercolor.Value;
            }
            if (cppheaderbackcolor.Value != "")
            {
                ecDetails.cppheaderbackcolor = cppheaderbackcolor.Value;
            }
            if (cpppayflowcolor.Value != "")
            {
                ecDetails.cpppayflowcolor = cpppayflowcolor.Value;
            }
            if (brandName.Value != "")
            {
                ecDetails.BrandName = brandName.Value;
            }

            request.SetExpressCheckoutRequestDetails = ecDetails;
        }


        // A helper method used by APIResponse.aspx that returns select response parameters 
        // of interest. 
        private void setKeyResponseObjects(PayPalAPIInterfaceServiceService service, SetExpressCheckoutResponseType setECResponse)
        {
            Dictionary<string, string> keyResponseParameters = new Dictionary<string, string>();
            keyResponseParameters.Add("API Status", setECResponse.Ack.ToString());
            if (setECResponse.Ack.Equals(AckCodeType.FAILURE) ||
                (setECResponse.Errors != null && setECResponse.Errors.Count > 0))
            {
                Session["Response_error"] = setECResponse.Errors;
            }
            else
            {
                Session["Response_error"] = null;
                keyResponseParameters.Add("EC token", setECResponse.Token);
                Session["Response_redirectURL"] = ConfigManager.Instance.GetProperty("paypalUrl")
                    + "_express-checkout&token=" + setECResponse.Token;
            }            
            Session["Response_keyResponseObject"] = keyResponseParameters;
            Session["Response_apiName"] = "SetExpressChecout";
            Session["Response_requestPayload"] = service.getLastRequest();
            Session["Response_responsePayload"] = service.getLastResponse();
            Response.Redirect("../APIResponse.aspx");
        }
    }
}
