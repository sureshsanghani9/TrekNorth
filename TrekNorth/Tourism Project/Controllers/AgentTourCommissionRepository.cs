using System.Collections.Generic;
using System.Globalization;
using System.Web.Configuration;
using MySql.Data.MySqlClient;
using Tourism_Project.Models;

namespace Tourism_Project.Controllers
{
    public class AgentTourCommissionRepository
    {
        private readonly MySqlConnection _connection;
        private MySqlCommand _cmd;
        private MySqlDataReader _dr;
        static readonly string ConnString = WebConfigurationManager.AppSettings["ServerDBConnection"].ToString(CultureInfo.InvariantCulture);

        public AgentTourCommissionRepository()
        {
            _connection = new MySqlConnection(ConnString);
            _cmd = new MySqlCommand();
            _dr = null;
        }



        public AgentTourCommission Get(int userId, int tourCodeId)
        {
            if (_connection.State == System.Data.ConnectionState.Open)
                _connection.Close();
            var commission = new AgentTourCommission { TourCodeID = tourCodeId, UserID = userId };
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = "Select Commission from agenttourcommission where userid='" + userId + "' and tourcodeid='" + tourCodeId + "' and active_f=1;";
            _dr = _cmd.ExecuteReader();

            while (_dr.Read())
            {
                commission.Commission = _dr.GetFloat(0);
            }
            _dr.Close();
            if (_connection.State == System.Data.ConnectionState.Open)
                _connection.Close();

            return commission;
        }
        
  


        public List<AgentTourCommission> GetList()
        {
            if (_connection.State == System.Data.ConnectionState.Open)
                _connection.Close();
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = " SELECT atc.userid, atc.tourcodeid, atc.commission, u.name as UserName, tc.tourcodevalues as TourCodeName FROM agenttourcommission atc inner join users u on atc.userid = u.id inner join tourcode tc on atc.tourcodeid = tc.id where active_f=1 ";
            _dr = _cmd.ExecuteReader();
            var models = new List<AgentTourCommission>();
            while (_dr.Read())
            {
                models.Add(new AgentTourCommission
                {
                    UserID = _dr.GetInt32(0),
                    TourCodeID = _dr.GetInt32(1),
                    Commission = _dr.GetFloat(2),
                    UserName = _dr.GetString(3),
                    TourCodeName = _dr.GetString(4)
                });
            }
            _dr.Close();
            return models;
        }


        public List<AgentTourCommission> GetList(int UserID)
        {
            if (_connection.State == System.Data.ConnectionState.Open)
                _connection.Close();
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = " SELECT atc.userid, atc.tourcodeid, atc.commission, u.name as UserName, tc.tourcodevalues as TourCodeName FROM agenttourcommission atc inner join users u on atc.userid = u.id inner join tourcode tc on atc.tourcodeid = tc.id where atc.active_f=1 and atc.userid=" + UserID.ToString();
            _dr = _cmd.ExecuteReader();
            var models = new List<AgentTourCommission>();
            while (_dr.Read())
            {
                models.Add(new AgentTourCommission
                {
                    UserID = _dr.GetInt32(0),
                    TourCodeID = _dr.GetInt32(1),
                    Commission = _dr.GetFloat(2),
                    UserName = _dr.GetString(3),
                    TourCodeName = _dr.GetString(4)
                });
            }
            _dr.Close();
            return models;
        }

        public void Insert(AgentTourCommission obj)
        {
            if (_connection.State == System.Data.ConnectionState.Open)
                _connection.Close();
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = " INSERT INTO agenttourcommission (`UserID`, `TourCodeID`, `Commission`,`active_f`) VALUES (@userId, @tourCodeId, @commission,@active_f); ";
            _cmd.Parameters.AddWithValue("@userId", obj.UserID);
            _cmd.Parameters.AddWithValue("@tourCodeId", obj.TourCodeID);
            _cmd.Parameters.AddWithValue("@commission", obj.Commission);

            _cmd.Parameters.AddWithValue("@active_f", obj.active_f); //on 04-08-2015
            _cmd.ExecuteNonQuery();
            _dr.Close();
        }


        public void Update(AgentTourCommission obj)
        {
            if (_connection.State == System.Data.ConnectionState.Open)
                _connection.Close();
            _connection.Open();
            _cmd = _connection.CreateCommand();
           // _cmd.CommandText = " UPDATE agenttourcommission SET `Commission`=@commission,`Active_f`=1 where userid=@userId  and tourcodeid=@tourCodeId; ";
            _cmd.CommandText = " UPDATE agenttourcommission SET `Commission`=@commission,`Active_f`=@active_f where userid=@userId and tourcodeid=@tourCodeId; ";
            _cmd.Parameters.AddWithValue("@userId", obj.UserID);
            _cmd.Parameters.AddWithValue("@tourCodeId", obj.TourCodeID);
            _cmd.Parameters.AddWithValue("@commission", obj.Commission);

            _cmd.Parameters.AddWithValue("@active_f", obj.active_f); //on 04-08-2015
            _cmd.ExecuteNonQuery();
            _dr.Close();
        }


        public void Delete(AgentTourCommission obj)
        {
            if (_connection.State == System.Data.ConnectionState.Open)
                _connection.Close();
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = " UPDATE agenttourcommission SET active_f=0 where userid=@userId and tourcodeid=@tourCodeId; ";
            _cmd.ExecuteNonQuery();
            _dr.Close();
        }


        public void DeleteAll(int UserID)
        {
            if (_connection.State == System.Data.ConnectionState.Open)
                _connection.Close();
            _connection.Open();
            _cmd = _connection.CreateCommand();
            _cmd.CommandText = " UPDATE agenttourcommission SET active_f=0 where userid=@userId; ";
            _cmd.ExecuteNonQuery();
            _dr.Close();
        }

    }
}