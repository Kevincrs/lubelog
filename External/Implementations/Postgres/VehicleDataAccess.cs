﻿using CarCareTracker.External.Interfaces;
using CarCareTracker.Models;
using Npgsql;
using System.Text.Json;

namespace CarCareTracker.External.Implementations
{
    public class PGVehicleDataAccess: IVehicleDataAccess
    {
        private NpgsqlConnection pgDataSource;
        private static string tableName = "vehicles";
        public PGVehicleDataAccess(IConfiguration config)
        {
            pgDataSource = new NpgsqlConnection(config["POSTGRES_CONNECTION"]);
            pgDataSource.Open();
            //create table if not exist.
            string initCMD = $"CREATE TABLE IF NOT EXISTS app.{tableName} (id INT GENERATED ALWAYS AS IDENTITY primary key, data jsonb not null)";
            using (var ctext = new NpgsqlCommand(initCMD, pgDataSource))
            {
                ctext.ExecuteNonQuery();
            }
        }
        public bool SaveVehicle(Vehicle vehicle)
        {
            if (string.IsNullOrWhiteSpace(vehicle.ImageLocation))
            {
                vehicle.ImageLocation = "/defaults/noimage.png";
            }
            if (vehicle.Id == default)
            {
                string cmd = $"INSERT INTO app.{tableName} (data) VALUES(CAST(@data AS jsonb)) RETURNING id";
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    ctext.Parameters.AddWithValue("data", "{}");
                    vehicle.Id = Convert.ToInt32(ctext.ExecuteScalar());
                    //update json data
                    if (vehicle.Id != default)
                    {
                        string cmdU = $"UPDATE app.{tableName} SET data = CAST(@data AS jsonb) WHERE id = @id";
                        using (var ctextU = new NpgsqlCommand(cmdU, pgDataSource))
                        {
                            var serializedData = JsonSerializer.Serialize(vehicle);
                            ctextU.Parameters.AddWithValue("id", vehicle.Id);
                            ctextU.Parameters.AddWithValue("data", serializedData);
                            return ctextU.ExecuteNonQuery() > 0;
                        }
                    }
                    return vehicle.Id != default;
                }
            } else
            {
                string cmd = $"UPDATE app.{tableName} SET data = CAST(@data AS jsonb) WHERE id = @id";
                using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
                {
                    var serializedData = JsonSerializer.Serialize(vehicle);
                    ctext.Parameters.AddWithValue("id", vehicle.Id);
                    ctext.Parameters.AddWithValue("data", serializedData);
                    return ctext.ExecuteNonQuery() > 0;
                }
            }
        }
        public bool DeleteVehicle(int vehicleId)
        {
            string cmd = $"DELETE FROM app.{tableName} WHERE id = @id";
            using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
            {
                ctext.Parameters.AddWithValue("id", vehicleId);
                return ctext.ExecuteNonQuery() > 0;
            }
        }
        public List<Vehicle> GetVehicles()
        {
            string cmd = $"SELECT id, data FROM app.{tableName} ORDER BY id ASC";
            var results = new List<Vehicle>();
            using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
            {
                using (NpgsqlDataReader reader = ctext.ExecuteReader())
                while (reader.Read())
                {
                    Vehicle vehicle = JsonSerializer.Deserialize<Vehicle>(reader["data"] as string);
                    results.Add(vehicle);
                }
            }
            return results;
        }
        public Vehicle GetVehicleById(int vehicleId)
        {
            string cmd = $"SELECT id, data FROM app.{tableName} WHERE id = @id";
            Vehicle vehicle = new Vehicle();
            using (var ctext = new NpgsqlCommand(cmd, pgDataSource))
            {
                ctext.Parameters.AddWithValue("id", vehicleId);
                using (NpgsqlDataReader reader = ctext.ExecuteReader())
                    while (reader.Read())
                    {
                        vehicle = JsonSerializer.Deserialize<Vehicle>(reader["data"] as string);
                    }
            }
            return vehicle;
        }
    }
}
