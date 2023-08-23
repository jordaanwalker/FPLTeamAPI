using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.FSharp.Control;
using Newtonsoft.Json;
using static FParsec.ErrorMessage;
using static MathNet.Numerics.Probability;
using static Microsoft.FSharp.Core.ByRefKinds;

public class Player
{
    public string web_name { get; set; } // Player Name
    public string element_type { get; set; } // Player Position - 1: GK 2: Def 3: Mid 4: Fwd
    public double now_cost { get; set; } // Player Cost
    public double influence { get; set; }
    public double creativity { get; set; } 
    public double threat { get; set; }
    
}

public class Root
{
    public List<Player> elements { get; set; }
}

class Program
{
    static readonly HttpClient client = new HttpClient();

    static async Task Main(string[] args)
    {
        HttpResponseMessage response = await client.GetAsync("https://fantasy.premierleague.com/api/bootstrap-static/");
        response.EnsureSuccessStatusCode();
        string responseBody = await response.Content.ReadAsStringAsync();

        Root playersData = JsonConvert.DeserializeObject<Root>(responseBody);


        // Sort Players based on Influence, Creativity & Threat: https://www.premierleague.com/news/65567

        //1.Influence
        //Influence evaluates the degree to which a player has made an impact on a single match or throughout the season.
        
        //It takes into account events and actions that could directly or indirectly effect the outcome of the fixture.
        
        //At the top level these are decisive actions like goals and assists. But the Influence score also processes significant defensive actions to analyse the effectiveness of defenders and goalkeepers.
        
        //2.Creativity
        //Creativity assesses player performance in terms of producing goalscoring opportunities for others.It can be used as a guide to identify the players most likely to supply assists.
        
        //While this analyses frequency of passing and crossing, it also considers pitch location and quality of the final ball.
        
        //3.Threat
        //This is a value that examines a player's threat on goal. It gauges the individuals most likely to score goals.
        
        //While attempts are the key action, the Index looks at pitch location, giving greater weight to actions that are regarded as the best chances to score.
        var sortedPlayers = playersData.elements.OrderByDescending(player => player.influence)
                                       .ThenByDescending(player => player.creativity)
                                       .ThenByDescending(player => player.threat)
                                       .ToList();

        // Sort players by position.
        var goalkeepers = sortedPlayers.Where(player => player.element_type == "1").ToList();
        var defenders = sortedPlayers.Where(player => player.element_type == "2").ToList();
        var midfielders = sortedPlayers.Where(player => player.element_type == "3").ToList();
        var forwards = sortedPlayers.Where(player => player.element_type == "4").ToList();

        // Total budget in Fantasy Premier League
        double budget = 100.0;

        //Set max number of players per position
        int maxPlayersDefender = 4;
        int maxPlayersMidfield = 5;
        int maxPlayersForward = 5;

        // Max of 15 players per team
        int totalPlayers = 15;

        List<Player> selectedTeam = new List<Player>();

        // Select players for each position
        selectedTeam.AddRange(goalkeepers.Take(1));
        selectedTeam.AddRange(defenders.Take(maxPlayersDefender));
        selectedTeam.AddRange(midfielders.Take(maxPlayersMidfield));
        selectedTeam.AddRange(forwards.Take(maxPlayersForward));

        // Calculate total cost
        double totalCost = selectedTeam.Sum(player => player.now_cost) / 10.0;

        // Ensure cost and total number of players in team are below maximum amount
        if (totalCost <= budget && selectedTeam.Count <= totalPlayers)
        {

            // Print selected team
            Console.WriteLine("Selected Team:");
            foreach (var player in selectedTeam)
            {
                Console.WriteLine($"{player.web_name} ({player.element_type}): Influence - {player.influence}, Creativity - {player.creativity}, Threat - {player.threat},  Cost - £{player.now_cost / 10.0}M");
            }
        }
        else
        {
            Console.WriteLine("Unable to create a team based on the provied parameters.");
        }
    }
}