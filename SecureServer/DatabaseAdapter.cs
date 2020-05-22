﻿using CommonTypes;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SecureServer
{
    internal class DatabaseAdapter
    {

        public SqlConnection connect = null;

        public void OpenConnection(string connectionString)
        {
            connect = new SqlConnection(connectionString);
            try
            {
                connect.Open();
            }
            catch (SqlException ex)
            {
                string create_database = "";
                string[] parameters = connectionString.Split(';');

                if (ex.ErrorCode == 4060)
                {
                    using (StreamReader tre = new StreamReader("database.txt"))
                    {
                        create_database = tre.ReadToEnd();
                    }
                    SqlConnection connection = new SqlConnection(parameters[0] + ";" + parameters[1] + ";");
                    connection.Open();
                    SqlCommand command = new SqlCommand();
                    command.Connection = connection;
                    command.CommandText = create_database;
                    command.ExecuteNonQuery();

                }
                Console.WriteLine(ex.Message);
            }

        }

        public void CloseConnection()
        {
            connect.Close();
        }

        public void AddEmployee(string data)
        {
            var csp = new SHA512CryptoServiceProvider();
            try
            {
                EmployeeContainer EC = JsonConvert.DeserializeObject<EmployeeContainer>(Encoding.UTF8.GetString(Convert.FromBase64String(data)));
                string sql = $"insert into Сотрудники(Имя,Фамилия,Отчество,[Id Должности],[Дата рождения],[Дата трудоустройства],[Адрес проживания],Прочее,Логин,Пароль,Фото,[Уровень доступа])" +
                    $" values ('{EC.Name}','{EC.Surname}','{EC.Patronymic}',(select top(1) id from Должность where Наименование='{EC.Profession}'),'{EC.Birthday}','{EC.DateOfEmployment}'," +
                    $"'{EC.Adress}','{EC.Other}',{(EC.Login == "" ? "null" : $"'{EC.Login}'")},{(EC.Password == "" ? "null" : $"'{Convert.ToBase64String(csp.ComputeHash(Encoding.UTF8.GetBytes(EC.Password)))}'")}," +
                    $"(@photo),{EC.AccessLevel})";
                var command = new SqlCommand(sql, connect);
                command.Parameters.Add("@photo", SqlDbType.Image, 1000000);
                command.Parameters["@photo"].Value = EC.Photo;
                command.ExecuteNonQuery();
            }
            catch (Exception) { }

        }
        public void EditEmployee(string data)
        {
            var csp = new SHA512CryptoServiceProvider();
            try
            {
                EmployeeContainer EC = JsonConvert.DeserializeObject<EmployeeContainer>(Encoding.UTF8.GetString(Convert.FromBase64String(data)));
                string sql = $"Update Сотрудники set Имя='{EC.Name}',Фамилия='{EC.Surname}',Отчество='{EC.Patronymic}',[Id Должности]=(select top(1) id from Должность where Наименование='{EC.Profession}')," +
                    $"[Дата рождения]='{EC.Birthday}',[Дата трудоустройства]='{EC.DateOfEmployment}',[Адрес проживания]='{EC.Adress}',Прочее='{EC.Other}',Логин={(EC.Login == "" ? "null" : $"'{EC.Login}'")}," +
                    $"Пароль={(EC.Password == "" ? "null" : $"'{Convert.ToBase64String(csp.ComputeHash(Encoding.UTF8.GetBytes(EC.Password)))}'")},Фото=(@photo),[Уровень доступа]={EC.AccessLevel}" +
                    $" where id={EC.Id}";

                var command = new SqlCommand(sql, connect);
                command.Parameters.Add("@photo", SqlDbType.Image, 1000000);
                command.Parameters["@photo"].Value = EC.Photo;
                command.ExecuteNonQuery();
            }
            catch (Exception) { }

        }
        public void DeleteEmployee(string id)
        {
            SqlCommand command = new SqlCommand($"delete from Сотрудники where id={id} ", connect);
            var transaction = connect.BeginTransaction();
            command.Transaction = transaction;
            try
            {
                command.ExecuteNonQuery();
                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
            }
        }
        public string[] ClientAutorizationData(string token)
        {

            string[] rezult = new string[3];
            string sql = $"select top(1) Наименование, [Key], [IP] from Токены t1 join Сотрудники t2 on t1.[Id Сотрудника]=t2.id inner join Должность t3 on t2.[Id Должности]=t3.id where Токен='{token}'";
            var table = QueryWithAnswer(sql);
            if (table.Rows.Count > 0)
            {
                rezult[0] = (string)table.Rows[0].ItemArray[0];
                rezult[1] = (string)table.Rows[0].ItemArray[1];
                rezult[2] = (string)table.Rows[0].ItemArray[2];
            }
            return rezult;
        }
        private DataTable QueryWithAnswer(string query)
        {
            DataTable rezultTable = new DataTable();
            using (SqlCommand cmd = new SqlCommand(query, connect))
            {
                try
                {
                    SqlDataReader dr = cmd.ExecuteReader();
                    rezultTable.Load(dr);
                    dr.Close();
                }
                catch (SqlException ex)
                {

                }
            }
            return rezultTable;
        }
        public void QueryWithoutAnswer(string query)
        {
            DataTable rezultTable = new DataTable();
            using (SqlCommand cmd = new SqlCommand(query, connect))
            {
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {

                }
            }
        }

        public EmployeeContainer[] GetEmployees()
        {
            DataTable rezultTable = QueryWithAnswer("select t1.id,Имя,Фамилия,Отчество,Наименование,[Дата рождения],[Дата трудоустройства],[Адрес проживания],Фото,Логин,[Уровень доступа], Прочее from Сотрудники t1 inner join Должность t2 on t1.[Id Должности]=t2.id;");
            var rezult = new EmployeeContainer[rezultTable.Rows.Count];
            for (int i = 0; i < rezultTable.Rows.Count; i++)
            {
                rezult[i] = new EmployeeContainer((int)rezultTable.Rows[i].ItemArray[0], (string)rezultTable.Rows[i].ItemArray[1],
                    (string)rezultTable.Rows[i].ItemArray[2], (string)rezultTable.Rows[i].ItemArray[3], (string)rezultTable.Rows[i].ItemArray[4],
                    ((DateTime)rezultTable.Rows[i].ItemArray[5]).ToShortDateString(), ((DateTime)rezultTable.Rows[i].ItemArray[6]).ToShortDateString(), (string)rezultTable.Rows[i].ItemArray[7],
                    (byte[])rezultTable.Rows[i].ItemArray[8], (rezultTable.Rows[i].ItemArray[9].GetType() == typeof(DBNull) ? "" : (string)rezultTable.Rows[i].ItemArray[9]), (byte)rezultTable.Rows[i].ItemArray[10],
                    (string)rezultTable.Rows[i].ItemArray[11]);
            }
            return rezult;
        }
        public StringContainer[] GetProfessions()
        {
            DataTable rezultTable = QueryWithAnswer("select Наименование from Должность");
            var rezult = new StringContainer[rezultTable.Rows.Count];
            for (int i = 0; i < rezultTable.Rows.Count; i++)
            {
                rezult[i] = new StringContainer((string)rezultTable.Rows[i].ItemArray[0]);
            }
            return rezult;
        }

        public void DeleteToken(string token)
        {
            QueryWithoutAnswer($"delete from Токены where Токен='{token}' ");
        }
        public void RefreshToken(string login, string token, string key, string IP)
        {
            QueryWithoutAnswer($"delete from Токены where [Id Сотрудника] =(select top(1) [id] from Сотрудники where Логин='{login}') ");
            QueryWithoutAnswer($"insert into Токены ( [Id Сотрудника] , [Токен] ,[Key],[Дата создания] ,[IP]) values ((select top(1) [id] from Сотрудники where Логин='{login}'),'{token}','{key}','{DateTime.Now.ToString()}','{IP}') ");
        }
        public string GetPasswordHash(string login)
        {
            DataTable rezultTable = QueryWithAnswer($"select Пароль from Сотрудники where Логин='{login}'");

            return rezultTable.Rows.Count != 0 ? rezultTable.Rows[0].ItemArray[0].ToString() : null;
        }
        public string GetRole(string login)
        {
            DataTable rezultTable = QueryWithAnswer($"select Наименование from Должность where id=(select top(1) [Id Должности] from Сотрудники where Логин='{login}')");

            return rezultTable.Rows[0].ItemArray[0].ToString();
        }

    }
}
