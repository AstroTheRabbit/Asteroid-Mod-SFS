using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using Newtonsoft.Json;
using SFS.World;
using SFS.World.Maps;

namespace AsteroidMod
{
    public class Asteroid : MonoBehaviour, I_Physics
    {
        public static GameObject AsteroidHolder;
        public string asteroidName;
        public WorldLocation location;
        public Physics physics;
        public Rigidbody2D rb2d;
        public float GM;
        public Vector2 dimensions;
        public List<float> points;
        public MapAsteroid mapAsteroid;

        Vector2 I_Physics.LocalPosition { get => transform.position; set => transform.position = value; }
        Vector2 I_Physics.LocalVelocity { get => rb2d.velocity; set => rb2d.velocity = value; }
        bool I_Physics.PhysicsMode
        {
            get
            {
                if (rb2d != null)
                {
                    return rb2d.simulated;
                }
                return false;
            }
            set => rb2d.simulated = value;
        }
        void I_Physics.OnFixedUpdate(Vector2 gravity) => ((I_Physics)this).LocalVelocity += gravity;

        void I_Physics.OnCrashIntoPlanet()
        {
            DestroyAsteroid(false);
        }

        public void DestroyAsteroid(bool createDust)
        {
            AsteroidManager.main.asteroids.Remove(this);
            if (createDust)
                Debug.Log("Asteroids Mod - Dust coming soon?");
        }
    }

    public class MapAsteroid : SelectableObject
    {
        public Asteroid asteroid;
        public MapIcon mapIcon;
        public override Location Location => asteroid.location.Value;
        public override Trajectory Trajectory => asteroid.physics.GetTrajectory();
        public override string EncounterText => SFS.Translations.Loc.main.Encounter;
        public override int OrbitDepth => Location.planet.orbitalDepth + 1;
        public override int ClickDepth => Location.planet.orbitalDepth + 1;
        public override double Navigation_Tolerance => 3 * asteroid.dimensions.magnitude;
        public override Vector3 Select_MenuPosition => MapDrawer.GetPosition(Location);
        public override string Select_DisplayName { get => asteroid.asteroidName; set => asteroid.asteroidName = value; }
        public override bool Select_CanRename => true;
        public override bool Select_CanNavigate => true;
        public override bool Select_CanEndMission => false;
	    public override string Select_EndMissionText => null;
        public override bool Select_CanFocus => true;
        public override bool Focus_FocusConditions(Double2 relativePosition, double viewDistance) => viewDistance > relativePosition.magnitude;

        private void Update()
        {
            mapIcon.SetRotation(asteroid.rb2d.rotation);
            // Debug.Log(asteroid.rb2d.transform.position);
            // Debug.Log("");
            // Debug.Log(asteroid.location.position.Value);
            // Debug.Log("-");
            // Debug.Log(asteroid.rb2d.velocity);
            // Debug.Log("");
            // Debug.Log(asteroid.location.velocity.Value);
            // Debug.Log("-----");
        }
    }

    [HarmonyPatch(typeof(MapManager), "DrawMap")]
    class MapDrawAsteroids
    {
        static void Postfix()
        {
            foreach (Asteroid asteroid in AsteroidManager.main.asteroids)
            {
                asteroid.mapAsteroid.Trajectory.DrawSolid(false, false);
            }
        }
    }

    [Serializable]
    [JsonConverter(typeof(WorldSave.LocationData.LocationConverter))]
    public class AsteroidSave
    {
        public string name;
        public WorldSave.LocationData location;
        public float rotation;
	    public float angularVelocity;
        public float GM;
        public Vector2 dimensions;
        public List<float> points;
        public AsteroidSave(Asteroid asteroid)
        {
            this.name = asteroid.asteroidName;
            this.location = new WorldSave.LocationData(asteroid.location.Value);
            this.rotation = asteroid.transform.rotation.eulerAngles.z;
            this.angularVelocity = asteroid.rb2d.angularVelocity;
            this.GM = asteroid.GM;
            this.dimensions = asteroid.dimensions;
            this.points = asteroid.points;
        }
        public AsteroidSave() {}
    }
}