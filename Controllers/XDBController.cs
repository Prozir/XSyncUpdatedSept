using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Data;
using System.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Configuration;
using Microsoft.Owin.Host.SystemWeb;

namespace XSync.Controllers
{
    public class XDBController : ApiController
    {

        private static string connString = ConfigurationManager.AppSettings.Get("connStr");
        private static string apisecret = ConfigurationManager.AppSettings.Get("apiSecret");
        private static string apiissuer = ConfigurationManager.AppSettings.Get("tokenIssuer");
        SqlConnection con = new SqlConnection(connString);

        [HttpGet]
        public Object GetToken() //get the user token
        {
            string key = apisecret;
            var issuer = apiissuer;
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var permClaims = new List<Claim>();
            permClaims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            permClaims.Add(new Claim("valid", "1"));
            permClaims.Add(new Claim("userid", "1"));
            permClaims.Add(new Claim("name", "PetPoojaPOS"));

            var token = new JwtSecurityToken(issuer,
                            issuer,
                            permClaims,
                            expires: DateTime.Now.AddDays(7),
                            signingCredentials: credentials);
            var jwt_token = new JwtSecurityTokenHandler().WriteToken(token);
            return new { data = jwt_token };
        }



        // POST: api/XDB/GetUserValues - to get all user values if authentication is successfull
        [Authorize]
        [HttpPost]
        public string GetPurchase()
        {            
                SqlDataAdapter da = new SqlDataAdapter("select * from SalesDetails", con);
                DataTable dt = new DataTable();
                da.Fill(dt);
                if (dt.Rows.Count > 0)
                {
                    return JsonConvert.SerializeObject(dt);
                }
                else
                {
                    return "Oops! No data available";
                }
        }

        // GET: api/XDB/5
       // public string Get(int id)
       // {
       //     return "value";
       // }

        // POST: api/XDB/InsertUserValues -- to insert values to db if authentication is successfull
       // [Authorize]
        [HttpPost]
        public string InsertPurchase([FromBody] JObject data)//string value)
        {                                     
            JObject propertiesobj = (JObject)data["properties"];

            // RestaurantID,OrderPaymentType,OrderTableNo,OrderPackagingCharge,TaxTitle1,TaxType1,TaxRate1,TaxAmount1,TaxTitle2,TaxType2,TaxRate2,TaxAmount2,
            // TaxTitle3,TaxType3,TaxRate3,TaxAmount3,OrderStatus,OnlineOrderNumber,CancelReason,DiscountTitle,DiscountType,DiscountRate,DiscountAmount

            string[] ColumnNames = new string[] { "RestaurantName","RestaurantAddress", "RestaurantContact","CustomerName", "CustomerAddress",
                "CustomerPhone","OrderId","OrderDeliveryCharge","OrderType","OrderNoOfPerson","OrderDiscountTotal","OrderTaxTotal","OrderRoundOff",
                "OrderCoreTotal","OrderTotal","OrderCreatedOn","OrderFrom","OrderSubOrderType","RestaurantID","OrderPaymentType","OrderTableNo","OrderPackagingCharge",
                "TaxTitle1","TaxType1","TaxRate1","TaxAmount1","TaxTitle2","TaxType2","TaxRate2","TaxAmount2","TaxTitle3","TaxType3","TaxRate3","TaxAmount3",
                "OrderStatus","OnlineOrderNumber","CancelReason","DiscountTitle","DiscountType","DiscountRate","DiscountAmount"};

                //restaurant details
                string resName = (string)propertiesobj.SelectToken("Restaurant.res_name");
                string resAddress = (string)propertiesobj.SelectToken("Restaurant.address");
                string resContactInfo = (string)propertiesobj.SelectToken("Restaurant.contact_information");
                string resID = (string)propertiesobj.SelectToken("Restaurant.restID");

                //Customer Details
                string custName = (string)propertiesobj.SelectToken("Customer.name");
                string custAddress = (string)propertiesobj.SelectToken("Customer.address");
                string custPhone = (string)propertiesobj.SelectToken("Customer.phone");

                //order details
                string odrID = (string)propertiesobj.SelectToken("Order.orderID");
                string odrDeliveryChrg = (string)propertiesobj.SelectToken("Order.delivery_charges");
                string odrType = (string)propertiesobj.SelectToken("Order.order_type");
                string odrPaymentType = (string)propertiesobj.SelectToken("Order.payment_type");
                string odrTblNo = (string)propertiesobj.SelectToken("Order.table_no");
                string odrNoOfPerson = (string)propertiesobj.SelectToken("Order.no_of_persons");
                string odrDiscountTotal = (string)propertiesobj.SelectToken("Order.discount_total");
                string odrTaxTotal = (string)propertiesobj.SelectToken("Order.tax_total");
                string odrRndOff = (string)propertiesobj.SelectToken("Order.round_off");
                string odrCoreTotal = (string)propertiesobj.SelectToken("Order.core_total");
                string odrTotal = (string)propertiesobj.SelectToken("Order.total");
                string odrCreatedOn = (string)propertiesobj.SelectToken("Order.created_on");
                string odrFrom = (string)propertiesobj.SelectToken("Order.order_from");
                string odrSubOdrType = (string)propertiesobj.SelectToken("Order.sub_order_type");
                string odrPackagingCharge = (string)propertiesobj.SelectToken("Order.packaging_charge");

            //not present in json
            string orderstatus = string.Empty;
            string onlineordernumber = string.Empty;
            string cancelreason = string.Empty;

            //discount details
            string discounttitle = string.Empty;
            string discounttype = string.Empty;
            string discountrate = string.Empty;
            string discountamount =  string.Empty;
            var alldiscountdetails = propertiesobj.SelectTokens("$.Discount", false).Children().ToArray();
            foreach (var x in alldiscountdetails)
            { 
                discounttitle= x["title"].ToString();
                discounttype= x["type"].ToString();
                discountrate = x["rate"].ToString();
                discountamount= x["amount"].ToString();
            }

                //tax details            
            string taxtitle1 = string.Empty, taxtype1 = string.Empty, taxrate1 = string.Empty, taxamount1 = string.Empty; //for sgstb
            string taxtitle2 = string.Empty, taxtype2 = string.Empty, taxrate2 = string.Empty, taxamount2 = string.Empty; //for gst1
            string taxtitle3 = string.Empty, taxtype3 = string.Empty, taxrate3 = string.Empty, taxamount3 = string.Empty; // for igst (if any)            
            var alltaxdetails = propertiesobj.SelectTokens("$.Tax", false).Children().ToArray();
            foreach (var x in alltaxdetails)
            {
                if(x["title"].ToString().ToLower().Contains("sgst") == true)
                {
                    taxtitle1 = x["title"].ToString();
                    taxtype1 = x["type"].ToString();
                    taxrate1 = x["rate"].ToString();
                    taxamount1 = x["amount"].ToString();
                }

                if (x["title"].ToString().ToLower().Contains("gst1") == true)
                {
                    taxtitle2 = x["title"].ToString();
                    taxtype2 = x["type"].ToString();
                    taxrate2 = x["rate"].ToString();
                    taxamount2 = x["amount"].ToString();
                }

                if (x["title"].ToString().ToLower().Contains("cgst") == true)
                {
                    taxtitle3 = x["title"].ToString();
                    taxtype3 = x["type"].ToString();
                    taxrate3 = x["rate"].ToString();
                    taxamount3 = x["amount"].ToString();
                }
            }            


            string[] ColumnValues = new string[] { resName, resAddress, resContactInfo, custName, custAddress, custPhone, odrID, odrDeliveryChrg,
                odrType,odrNoOfPerson,odrDiscountTotal,odrTaxTotal,odrRndOff,odrCoreTotal,odrTotal,odrCreatedOn,odrFrom,odrSubOdrType,resID,odrPaymentType,odrTblNo,odrPackagingCharge,
                taxtitle1,taxtype1,taxrate1,taxamount1,taxtitle2,taxtype2,taxrate2,taxamount2,taxtitle3,taxtype3,taxrate3,taxamount3,orderstatus,onlineordernumber,cancelreason,
                discounttitle,discounttype,discountrate,discountamount};

                //string insertStr = "Insert into PurchaseDetails(";
                StringBuilder insertStr = new System.Text.StringBuilder("Insert into SalesDetails(");
                int cntA = 0;
                int cntB = 0;
                foreach (string colname in ColumnNames)
                {
                    cntA++;
                    if (cntA < ColumnNames.Length)
                    {
                        insertStr.Append(colname + ",");
                    }
                    else if(cntA == ColumnNames.Length)
                    insertStr.Append(colname + ") output INSERTED.SalesId VALUES(");
                }
                foreach (string colval in ColumnValues)
                {
                    cntB++;                    
                    if (cntB < ColumnValues.Length)
                    {                         
                        insertStr.Append("('"+colval.Replace("'","''")+"')" + ",");                       
                    }
                    else if (cntB == ColumnValues.Length)
                    {                        
                        insertStr.Append("('" + colval.Replace("'", "''") + "')" + ")");
                    }
                       
                }

                //SqlCommand cmd = new SqlCommand("Insert into MyTable(PersonID,LastName,City) VALUES(" + personid + ",'" + lastname + "'," + "'" + city + "')", con);
                SqlCommand cmd = new SqlCommand(insertStr.ToString(), con);
                con.Open();
                //int i = cmd.ExecuteNonQuery();
                int salesid = (int)cmd.ExecuteScalar();
                con.Close();

            //Order item details to be stored in SalesOrderItem table
                bool orderiteminsert = true;
                var orderitemdetails = propertiesobj.SelectTokens("$.OrderItem", false).Children().ToArray();
               
                foreach (var x in orderitemdetails)
                {
                    //insert row
                    SqlCommand orderitemcmd = new SqlCommand("Insert into SalesOrderItem VALUES ('" + salesid.ToString() + "','" + x["name"].ToString() + "','" + x["itemid"].ToString() + "','" +
                        x["itemcode"].ToString() + "','" + x["vendoritemcode"].ToString() + "','" + x["specialnotes"].ToString() + "','" + x["price"].ToString() + "','" + x["quantity"].ToString() + "','" +
                        x["total"].ToString() + "','" + "Moved to AddOn table" + "','" + x["category_name"].ToString() + "','" + x["sap_code"].ToString() + "')", con);
                    con.Open();
                    int i = orderitemcmd.ExecuteNonQuery();
                    con.Close();                    
                    if (i == 1)
                    {

                        // order item add on details to be stored in SalesOrderItemAddOn table
                        con.Open();
                        for ( int cnt = 0;cnt< x["addon"].ToArray().Length;cnt++)
                        {
                            SqlCommand orderitemaddoncmd = new SqlCommand("Insert into SalesOrderItemAddOn VALUES ('" + salesid.ToString() + "','" + x["itemid"].ToString() + "','" + x["addon"][cnt]["group_name"].ToString() + "','" +
                            x["addon"][cnt]["name"].ToString() + "','" + x["addon"][cnt]["price"].ToString() + "','" + x["addon"][cnt]["quantity"].ToString() + "','" + x["addon"][cnt]["sap_code"].ToString() + "')", con);
                            orderitemaddoncmd.ExecuteNonQuery();
                        } 
                        con.Close();                                        
                        orderiteminsert = true;
                    }
                    else
                    {
                        orderiteminsert = false;
                    }
                }
           
            if (salesid.ToString()!=null || salesid.ToString()!="0" && orderiteminsert==true)
                {
                    return "Record inserted successfully";
                }
                else
                {
                    return "A runtime error is encountered.";
                }

        }
       
    }
}
