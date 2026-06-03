using System;
using AeroSimulator.Controllers;

class Program
{
    static void Main(string[] args)
    {
        // Inicjalizacja i odpalenie całej symulacji
        FlightController controller = new FlightController();
        controller.StartSimulation();
    }
}