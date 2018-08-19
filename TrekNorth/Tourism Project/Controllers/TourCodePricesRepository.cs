using System.Collections.Generic;
using System.Globalization;
using System.Web.Configuration;
using MySql.Data.MySqlClient;
using Tourism_Project.Models;

namespace Tourism_Project.Controllers
{
    public class TourCodePricesRepository
    {

        protected static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private readonly MySqlConnection _connection;
        private MySqlCommand _cmd;
        private MySqlDataReader _dr;
        static readonly string ConnString = WebConfigurationManager.AppSettings["ServerDBConnection"].ToString(CultureInfo.InvariantCulture);

        public TourCodePricesRepository()
        {
            _connection = new MySqlConnection(ConnString);
            _cmd = new MySqlCommand();
            _dr = null;
        }



        public TourCodePrice Get(int tourCodeId)
        {
            if (_connection.State == System.Data.ConnectionState.Open)
                _connection.Close();
            var price = new TourCodePrice { TourCodeID = tourCodeId };
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = "Select price, pricechild, pricefamilychild, goldprice, goldpricechild, goldpricefamilychild from tourcodeprices where tourcodeid='" + tourCodeId + "' and active_f=1;";
            _dr = _cmd.ExecuteReader();

            while (_dr.Read())
            {
                price.Price = _dr.GetFloat(0);
                price.PriceChild = _dr.GetFloat(1);
                price.PriceFamilyChild = _dr.GetFloat(2);
                price.GoldPrice = _dr.GetFloat(3);
                price.GoldPriceChild = _dr.GetFloat(4);
                price.GoldPriceFamilyChild = _dr.GetFloat(5);
            }
            _dr.Close();
            if (_connection.State == System.Data.ConnectionState.Open)
                _connection.Close();

            return price;
        }


        public List<TourCodePrice> GetList()
        {
            if (_connection.State == System.Data.ConnectionState.Open)
                _connection.Close();
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText =
               "  SELECT tc.id, IFNULL(tcp.price,0), tc.tourcodevalues as TourCodeName, tn.tourname, IFNULL(tcp.pricechild,0), IFNULL(tcp.pricefamilychild,0), IFNULL(tcp.goldprice,0), IFNULL(tcp.goldpricechild,0), IFNULL(tcp.goldpricefamilychild,0)   " +
               "  FROM tourcode tc                                                                    " +
               "  left outer join tourcodeprices tcp on tc.id = tcp.tourcodeid                        " +
               "  inner join tournames tn on tn.id = tourid                                           " +
               "  order by tn.id                                                                      ";
            _dr = _cmd.ExecuteReader();
            var models = new List<TourCodePrice>();
            while (_dr.Read())
            {
                models.Add(new TourCodePrice
                {
                    TourCodeID = _dr.GetInt32(0),
                    Price = _dr.GetFloat(1),
                    TourCodeName = _dr.GetString(2),
                    TourName = _dr.GetString(3),
                    PriceChild = _dr.GetFloat(4),
                    PriceFamilyChild = _dr.GetFloat(5),
                    GoldPrice = _dr.GetFloat(6),
                    GoldPriceChild = _dr.GetFloat(7),
                    GoldPriceFamilyChild = _dr.GetFloat(8)
                });
            }
            _dr.Close();
            return models;
        }


        public void Insert(TourCodePrice obj)
        {
            if (_connection.State == System.Data.ConnectionState.Open)
                _connection.Close();
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = " INSERT INTO tourcodeprices (`TourCodeID`, `Price`, `PriceChild`, `PriceFamilyChild`, `GoldPrice`, `GoldPriceChild`, `GoldPriceFamilyChild`) VALUES (@tourCodeId, @price, @pricechild, @pricefamilychild, @goldprice, @goldpricechild, @goldpricefamilychild); ";
            _cmd.Parameters.AddWithValue("@tourCodeId", obj.TourCodeID);
            _cmd.Parameters.AddWithValue("@price", obj.Price);
            _cmd.Parameters.AddWithValue("@pricechild", obj.PriceChild);
            _cmd.Parameters.AddWithValue("@pricefamilychild", obj.PriceFamilyChild);
            _cmd.Parameters.AddWithValue("@goldprice", obj.GoldPrice);
            _cmd.Parameters.AddWithValue("@goldpricechild", obj.GoldPriceChild);
            _cmd.Parameters.AddWithValue("@goldpricefamilychild", obj.GoldPriceFamilyChild);
            _cmd.ExecuteNonQuery();
            _dr.Close();
        }


        public void Update(TourCodePrice obj)
        {
            if (_connection.State == System.Data.ConnectionState.Open)
                _connection.Close();
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = " UPDATE tourcodeprices SET `Price`=@price,`PriceChild`=@pricechild,`PriceFamilyChild`=@pricefamilychild,`GoldPrice`=@goldprice,`GoldPriceChild`=@goldpricechild,`GoldPriceFamilyChild`=@goldpricefamilychild where tourcodeid=@tourCodeId; ";
            _cmd.Parameters.AddWithValue("@tourCodeId", obj.TourCodeID);
            _cmd.Parameters.AddWithValue("@price", obj.Price);
            _cmd.Parameters.AddWithValue("@pricechild", obj.PriceChild);
            _cmd.Parameters.AddWithValue("@pricefamilychild", obj.PriceFamilyChild);
            _cmd.Parameters.AddWithValue("@goldprice", obj.GoldPrice);
            _cmd.Parameters.AddWithValue("@goldpricechild", obj.GoldPriceChild);
            _cmd.Parameters.AddWithValue("@goldpricefamilychild", obj.GoldPriceFamilyChild);
            _cmd.ExecuteNonQuery();
            _dr.Close();
        }

        public void UpdateList(List<TourCodePrice> list)
        {
            var lineCount = 0;
            try
            {
                string query = "";
                foreach (var obj in list)
                {
                    lineCount++;
                    if (_connection.State == System.Data.ConnectionState.Open)
                        _connection.Close();

                    lineCount++;
                    _connection.Open();
                    lineCount++;

                    _cmd = _connection.CreateCommand();
                    _cmd.CommandText = "Select price, pricechild, pricefamilychild, goldprice, goldpricechild, goldpricefamilychild from tourcodeprices where tourcodeid='" + obj.TourCodeID + "' and active_f=1;";
                    _dr = _cmd.ExecuteReader();

                    lineCount++;

                    bool isNew = false;
                    while (_dr.Read())
                    {
                        isNew = true;
                    }

                    if (!isNew)
                    {
                        query += " INSERT INTO tourcodeprices (`TourCodeID`, `Price`, `PriceChild`, `PriceFamilyChild`, `GoldPrice`, `GoldPriceChild`, `GoldPriceFamilyChild`, `active_f`) VALUES (" + obj.TourCodeID + ", " + obj.Price.Value + ", " + obj.PriceChild + ", " + obj.PriceFamilyChild + ", " + obj.GoldPrice.Value + ", " + obj.GoldPriceChild + ", " + obj.GoldPriceFamilyChild + ", 1); ";
                    }
                    else
                    {
                        query += " UPDATE tourcodeprices SET `Price`=" + obj.Price.Value + ",`PriceChild`=" + obj.PriceChild + ",`PriceFamilyChild`=" + obj.PriceFamilyChild + ", `GoldPrice`=" + obj.GoldPrice.Value + ",`GoldPriceChild`=" + obj.GoldPriceChild + ",`GoldPriceFamilyChild`=" + obj.GoldPriceFamilyChild + " where tourcodeid=" + obj.TourCodeID + "; ";
                    }
                    
                    lineCount++;
                    if (_connection.State == System.Data.ConnectionState.Open)
                        _connection.Close();
                }
                lineCount++;
                _connection.Open();
                lineCount++;
                _cmd = _connection.CreateCommand();
                lineCount++;
                _cmd.CommandText = query;
                lineCount++;
                _cmd.ExecuteNonQuery();
                lineCount++;
            }
            catch (System.Exception ex)
            {
                log.Error(ex.Message + ":" + ex.InnerException + " : on " + lineCount);
            }
        }


        public void Delete(TourCodePrice obj)
        {
            if (_connection.State == System.Data.ConnectionState.Open)
                _connection.Close();
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = " DELETE from tourcodeprices where tourcodeid=@tourCodeId; ";
            _cmd.Parameters.AddWithValue("@tourCodeId", obj.TourCodeID);
            _cmd.ExecuteNonQuery();
            _dr.Close();
        }

    }
}