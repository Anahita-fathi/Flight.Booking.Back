namespace Flight.Booking.Back;

using System.IO;
using System.Text.Json;

public static class ReservationFileHelper
{
    private static readonly string FilePath = "reservations.json"; 

    
    public static List<Reservation> GetReservations()
    {
        if (!File.Exists(FilePath))
        {
            return new List<Reservation>();  
        }

        var jsonData = File.ReadAllText(FilePath);
        return JsonSerializer.Deserialize<List<Reservation>>(jsonData) ?? new List<Reservation>();
    }


    public static void SaveReservations(List<Reservation> reservations)
    {
        var jsonData = JsonSerializer.Serialize(reservations, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(FilePath, jsonData);
    }
}
public class Reservation
{
    public int Id { get; set; }
    public string Username { get; set; }
    public int FlightId { get; set; }
    public DateTime ReservationDate { get; set; }
    public string Status { get; set; }
}
