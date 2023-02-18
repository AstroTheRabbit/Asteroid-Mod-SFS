using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using ModLoader;
using ModLoader.Helpers;
using SFS.World;

namespace AsteroidMod
{
    public class Main : Mod
    {
        public override string ModNameID => "asteroidmod";
        public override string DisplayName => "SFS Asteroids";
        public override string Author => "pixelgaming579";
        public override string MinimumGameVersionNecessary => "1.5.8";
        public override string ModVersion => "v1.0";
        public override string Description => "Spawns a bunch of moveable asteroids from JPL's Small-Body Database (SBDB).";

        public static Main main;
        const string mapIconBase64 = @"iVBORw0KGgoAAAANSUhEUgAAAHgAAAB4CAYAAAA5ZDbSAAAACXBIWXMAAA7EAAAOxAGVKw4bAAAEQ0lEQVR4nO2d3VHbUBCFz0PeoYO4A0IFqIM4FURUAFQQUUGggogOoAPRAXQgOoAKyN1Ze+IQXf1ZkqWT883s2LmSbMefd7VXP8On9/d3CF4+QVAjweRIMDkSTM6hBJ+E+BJiFSLZGU+wfIqd568hnnYeH6f+MFMJPoLLW28eV+Al+fDv9c5zE11sIg/xNvaHGVvw5xCXIdIQxxD2Haw3cQOXbI/PY73hWILP4FJTiDrSTRTwRBhc9NCCrRTbLzKF6EIC30ffw7+7wUr3kIK/wkuOSnF/rHSXcMkPQ7zgUIJ/wkuM2B9LEMvkLMT1vi+2r+CjzYdJIIYmg882LHF6l+x9BNtcNofPZ8U4pPDvN0FPyX0FbzN3BTE2JjgP8a3Pxn0Em9wCkjsl1nz9CnHedcM+gnOoLB+CFN5hd2q8ugr+gb8PvYlpyeCS79pu0EXw2eYNxGGxA0lFiJc2K7cVvG2qxOHZzpNP26zcVvAldIRqTlgPdBHitmnFNoLtjFAGMTcyeCbXluo2gjOIOWIVNUPD1KlJsGVvCjFXUrjkaBY3Cc4g5o71R1exhXWCrXPWnHf+pOgp2OSqc54/5ug7Igc/mgSLZZBAgqkxV5XddEzwGcSSsDJtzv657jomWGeLloc5k2BiKp3FBK8glsaqajAmOIFYGknVoO4uJEeCyZFgciSYHAkmR4LJkWByJJgcCSZHgsmRYHIkmBwJJkeCyZFgciSYHAkmR4LJkWByJJgcCSZHgsmRYHIkmBwJJkeCyZFgciSYHAkmR4LJkWByJJgcCSZHgsmRYHIkmBwJJkeCyZFgciSYHAkmR4LJkWByJJgcCSZHgsmRYHIkmBwJJkeCyZFgciSYnJjgEvrLK0ujrBqUYB7KqsE6wWJZlFWDEsxDWTUYE1xALI2iajAm+AliaTxWDcYEv8El649ULoMitqBuHpyHuIFYAnlsQZ3ge0jwUriPLagT/AJP/QRizpjct9jCpkOVlsEJxJyprbJNgh+gLJ4zBSLd85Y2JxsyaF48V9KmFdoItl9IAWXx3LDS/NK0UtvThSl8XnwMMQdKeGVtpK1g+6Vcoma+JSZljZrOeZcuJ/zv4GU6hTgklmjPbVfuekWHvfgK2h8fijzEbZcNugq2smDloYCOU09NHuK860Z9rskyyQn8CEoCMQXWMV/12bDvRXfbTLY3TiHGJIX3P73Y56pKk2wl4xW+bxbDk2EPucYQl81a6bA5smWz5snDYUnTqaGqYqjrou1XVsD3y2q+9sMqou3+HjEAQ174bgdDTkNcwEuLsrk7WYjrIV9wjDsbrKzkUAPWhQIdD2C0ZaxbV7YNWAb/4GrCqingifAw1huMfW+SlW1rwjL4fiWF5s4lvFfJMULGfmSqm88so+/wp+U/gQu3hmwF7sasgEt92jwfXeouh7q78BkT/0f/V3T7KDkSTI4EkyPB5PwGk8ufS15io4IAAAAASUVORK5CYII=";
        public override void Early_Load()
        {
            new Harmony(ModNameID).PatchAll();
            main = this;
        }

        public override void Load()
        {
            Asteroid.AsteroidHolder = new GameObject("Asteroid Holder");
            Asteroid.AsteroidHolder.AddComponent<AsteroidManager>();
            GameObject.DontDestroyOnLoad(Asteroid.AsteroidHolder);

            Texture2D tex = CreateTextureFromBase64(mapIconBase64, TextureFormat.RGBA32);
            AsteroidManager.main.mapIconSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 120f);
    
            SceneHelper.OnWorldSceneLoaded += CreateAsteroids;
            void CreateAsteroids(UnityEngine.SceneManagement.Scene scene)
            {
                SelectorSettings ss = new SelectorSettings();
                ss.amount = 1;

                AsteroidManager.main.DownloadAndCreateAsteroids(ss);

            }

            ModLoader.IO.Console.commands.Add
            (
                (string input) =>
                {
                    if (input != "checkmap")
                        return false;

                    foreach (var rocket in GameManager.main.rockets)
                    {
                        rocket.mapIcon.mapIcon.GetComponent<SpriteRenderer>().color = Color.red;
                        List<object> output = new List<object>()
                        {
                            rocket.rocketName,
                            rocket.physics.PhysicsMode,
                            rocket.rb2d.velocity,
                            rocket.physics.location.velocity.Value,
                            rocket.mapIcon.mapIcon.transform.position,
                            rocket.mapIcon.mapIcon.transform.localPosition,
                            rocket.mapIcon.mapIcon.transform.rotation.eulerAngles,
                            rocket.mapIcon.mapIcon.GetComponent<SpriteRenderer>().color,
                        };
                        Debug.Log("\n\n" + string.Join("\n", output));
                    }

                    Debug.Log("Asteroids below");
                    foreach (var asteroid in AsteroidManager.main.asteroids)
                    {
                        List<object> output = new List<object>()
                        {
                            asteroid.asteroidName,
                            asteroid.physics.PhysicsMode,
                            asteroid.rb2d.velocity,
                            asteroid.physics.location.velocity.Value,
                            asteroid.mapAsteroid.mapIcon.transform.position,
                            asteroid.mapAsteroid.mapIcon.transform.localPosition,
                            asteroid.mapAsteroid.mapIcon.transform.rotation.eulerAngles,
                            asteroid.mapAsteroid.mapIcon.GetComponent<SpriteRenderer>().color,
                        };
                        Debug.Log("\n\n" + string.Join("\n", output));
                    }

                    return false;
                }
            );
        }
        public static Texture2D CreateTextureFromBase64(string base64Image, TextureFormat textureFormat)
        {
            byte[] data = Convert.FromBase64String(base64Image);
            Texture2D result = new Texture2D(1, 1, textureFormat, 0, true);
            result.LoadImage(data);
            return result;
        }
    }
}