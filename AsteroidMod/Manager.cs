using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using SFS.World;
using SFS.World.Maps;
using SFS.Parsers.Regex;

namespace AsteroidMod
{
    public class AsteroidManager : MonoBehaviour
    {
        public static AsteroidManager main;
        public Sprite mapIconSprite;
        public List<Asteroid> asteroids = new List<Asteroid>();
        public AsteroidManager() => main = this;

        public Asteroid CreateAsteroid(string name, Location location, float GM, Vector2 dimensions)
        {
            GameObject go = new GameObject(name);
            go.transform.parent = this.transform;
            Asteroid asteroid = go.AddComponent<Asteroid>();
            asteroids.Add(asteroid); 

            asteroid.asteroidName = name;
            asteroid.GM = GM;
            asteroid.dimensions = dimensions;
            asteroid.location = asteroid.GetOrAddComponent<WorldLocation>();
            asteroid.location.Value = location;
            asteroid.rb2d = asteroid.GetOrAddComponent<Rigidbody2D>();
            asteroid.rb2d.velocity = asteroid.location.velocity.Value;

            asteroid.physics = asteroid.GetOrAddComponent<Physics>();
            asteroid.physics.loader = asteroid.GetOrAddComponent<WorldLoader>();
            asteroid.physics.location = asteroid.physics.loader.location = asteroid.location;
            asteroid.physics.loader.holder = asteroid.gameObject;
            asteroid.physics.loader.loadDistance = 5000 + asteroid.dimensions.magnitude;
            asteroid.physics.SetLocationAndState(location, false);

            asteroid.mapAsteroid = new GameObject(asteroid.name + " - Map Icon").GetOrAddComponent<MapAsteroid>();
            asteroid.mapAsteroid.transform.parent = this.transform;
            asteroid.mapAsteroid.asteroid = asteroid;
            asteroid.mapAsteroid.mapIcon = asteroid.mapAsteroid.GetOrAddComponent<MapIcon>();
            asteroid.mapAsteroid.mapIcon.location = asteroid.location;
            asteroid.mapAsteroid.mapIcon.mapIcon = asteroid.mapAsteroid.mapIcon.gameObject;
            asteroid.transform.localScale = asteroid.mapAsteroid.mapIcon.mapIcon.transform.localScale = Vector3.one;

            SpriteRenderer sr = asteroid.mapAsteroid.GetOrAddComponent<SpriteRenderer>();
            sr.sprite = mapIconSprite;
            sr.color = Color.white;
            sr.sortingLayerName = "Map";
            sr.material = new Material(Shader.Find("SFS/Map Icon"));

            return asteroid;
        }

        public Asteroid LoadAsteroid(AsteroidSave save)
        {
            return CreateAsteroid(save.name, save.location.GetSaveLocation(WorldTime.main.worldTime), save.GM, save.dimensions);
        }

        public async void DownloadAndCreateAsteroids(SelectorSettings settings)
        {
            APIQueryResponse response = await AsteroidDownloader.SendQuery(settings, "full_name,a,e,w,GM,diameter,extent", true);
            double AUtoScaledM(double au) => (au / 20) * 1.496e+11;
            SimpleRegex re = new SimpleRegex(@"(?<x>\d+\.?\d+?) ?x ?(?<y>\d+\.?\d+?)");

            Vector2 ParseExtent(string extent)
            {
                if (!re.Input(extent))
                    throw new UnityException("Asteroid Mod - Coundn't parse extent field!");
                return new Vector2(float.Parse(re.GetGroup("x").Value), float.Parse(re.GetGroup("y").Value));
            }

            foreach (var data in response.data)
            {
                string name = response.GetFieldFromData<string>("full_name", data);
                double diameter = response.GetFieldFromData<double>("diameter", data);

                double GM;
                if (response.GetFieldFromData<object>("GM", data) != null)
                    GM = response.GetFieldFromData<double>("GM", data);
                else
                    GM = Math.Pow(diameter, 0.5);

                double sma = AUtoScaledM(response.GetFieldFromData<double>("a", data));
                double ecc = response.GetFieldFromData<double>("e", data);
                double arg = response.GetFieldFromData<double>("w", data) * Mathf.Deg2Rad;

                Vector2 dimensions;
                if (response.GetFieldFromData<object>("extent", data) != null)
                    dimensions = ParseExtent(response.GetFieldFromData<string>("extent", data));
                else
                    dimensions = new Vector2((float)diameter, (float)diameter);


                Orbit orbit = new Orbit(sma, ecc, arg, -1, SFS.Base.planetLoader.planets.Values.First(p => p.parentBody == null), PathType.Eternal, null);

                CreateAsteroid(name, orbit.GetLocation(WorldTime.main.worldTime), (float)GM, dimensions);
            }
        }
    }
}