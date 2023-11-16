using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace TileGame
{
    public class _constants
    {
        public const int screenWidth = 1280;
        public const int screenHeight = 720;
        public const float PI = 3.14159f;
    }

    public class Camera
    {
        public Vector3 position; //position
        private Vector3 pry; //pitch roll yaw

        public void Pitch(float amount)
        {
            pry.X += amount;
        }

        public void Roll(float amount)
        {
            pry.Y += amount;
        }

        public void Yaw(float amount)
        {
            pry.Z += amount;
        }

        public void SetPosition(Vector3 newPos)
        {
            position = newPos;
        }

        public Vector3 GetPosition()
        {
            return position;
        }

        public void SetPitch(float pitch)
        {
            pry.X = pitch;
        }

        public void SetRoll(float roll)
        {
            pry.Y = roll;
        }

        public void SetYaw(float yaw)
        {
            pry.Z = yaw;
        }

        public Matrix GetView()
        {
            Vector4 forward = new Vector4(0f, 0f, -1f, 1f);
            Matrix sca = Matrix.CreateScale(1f);
            Matrix pos = Matrix.CreateTranslation(position.X, position.Y, position.Z);
            forward = Vector4.Transform(forward, sca * Matrix.CreateFromYawPitchRoll(pry.Z, pry.X, pry.Y) * pos);
            return Matrix.CreateLookAt(position, new Vector3(forward.X, forward.Y, forward.Z), new Vector3(0f, 1f, 0f));
        }

        public Matrix GetWorld()
        {
            Matrix sca = Matrix.CreateScale(1f);
            Matrix rot = Matrix.CreateFromYawPitchRoll(pry.Z, pry.X, pry.Y);
            Matrix pos = Matrix.CreateTranslation(position);
            return sca * rot * pos;
        }
    }

    public class GameObject
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    public class Piece : GameObject
    {
        private Model modelRef;
        public Piece(Model loadedPieceModel)
        {
            modelRef = loadedPieceModel;
            position = new Vector3(0f, 0f, 0f);
            rotation = Quaternion.Identity;
            scale = new Vector3(1f, 1f, 1f);
        }

        public void Draw(Matrix vMat, Matrix pMat)
        {
            foreach (ModelMesh mesh in modelRef.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    Quaternion rotQuat = new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W); //convert glmsharp quaternion to xna quaternion
                    Matrix wMat = Matrix.CreateScale(scale.X, scale.Y, scale.Z) * Matrix.CreateFromQuaternion(rotQuat) * Matrix.CreateTranslation(position.X, position.Y, position.Z);
                    effect.World = wMat;
                    effect.View = vMat;
                    effect.Projection = pMat;
                }

                mesh.Draw();
            }
        }
    }

    public class Board
    {
        private Piece[,] pieces;
        private int size;

        public Board(int boardSize, Model loadedPieceModel)
        {
            size = boardSize;
            pieces = new Piece[size, size];
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    pieces[x, y] = new Piece(loadedPieceModel);
                    pieces[x, y].position = new Vector3(x * 1.5f - 1.5f, y * 1.5f, 0f);
                    pieces[x, y].scale = new Vector3(0.25f);
                }
            }
        }

        public void Draw(Matrix vMat, Matrix pMat)
        {
            foreach (Piece piece in pieces)
            {
                piece.Draw(vMat, pMat);
            }
        }

        public void Click(Vector2 mousePos, Camera cam, GameWindow window)
        {
            Matrix pMat = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(56f), (float)_constants.screenWidth / (float)_constants.screenHeight, 0.1f, 100f);
            Vector2 NDC = mousePos / new Vector2(_constants.screenWidth, _constants.screenHeight); //transform to range 0-1
            NDC *= 2; //.. 0-2
            NDC -= new Vector2(1f); //.. -1-1
            Vector4 screenPos = new Vector4(NDC.X, -NDC.Y, 1f, 1f);
            Matrix inverse = pMat * cam.GetView();
            inverse = Matrix.Invert(inverse);
            Vector4 worldPos = Vector4.Transform(screenPos, inverse);
            worldPos /= worldPos.W;
            window.Title = worldPos.X.ToString();
            foreach (Piece piece in pieces)
            {
                if (worldPos.X >= piece.position.X)
                {
                    piece.scale = new Vector3(0.3f);
                }
                else
                {
                    piece.scale = new Vector3(0.25f);
                }
            }
        }
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Camera camera;
        private float deltaTime = 0;
        private List<Model> renderObjects = new List<Model>();
        Vector2 mousePos;
        private Matrix projection;

        private Model cube;
        private Board board;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();

            _graphics.PreferredBackBufferWidth = _constants.screenWidth;
            _graphics.PreferredBackBufferHeight = _constants.screenHeight;
            _graphics.ApplyChanges();

            camera = new Camera();
            //camera.SetYaw(-constants.PI);
            camera.SetPosition(new Vector3(0, 1, 10));

            Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(56f), (float)_constants.screenWidth / (float)_constants.screenHeight, 0.1f, 100f);

            board = new Board(3, cube);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            cube = Content.Load<Model>("cube");
            renderObjects.Add(cube);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            MouseState mState = Mouse.GetState(); //get mouse state
            mousePos.X = mState.Position.X; //update mouse position
            mousePos.Y = mState.Position.Y; //..

            if (Keyboard.GetState().IsKeyDown(Keys.A))
            {
                camera.Yaw(-0.05f * deltaTime);
            }
            if (Keyboard.GetState().IsKeyDown(Keys.D))
            {
                camera.Yaw(0.05f * deltaTime);
            }
            if (Keyboard.GetState().IsKeyDown(Keys.W))
            {
                camera.Pitch(-0.05f * deltaTime);
            }
            if (Keyboard.GetState().IsKeyDown(Keys.S))
            {
                camera.Pitch(0.05f * deltaTime);
            }

            if (mState.LeftButton == ButtonState.Pressed)
            {
                board.Click(mousePos, camera, Window);
            }

            base.Update(gameTime);
            deltaTime = (float) gameTime.ElapsedGameTime.TotalSeconds; //calculate delta time in seconds
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue); //clear with blue colour

            // TODO: Add your drawing code here
            Matrix world = Matrix.CreateTranslation(0f, 0f, 0f);
            Matrix view = camera.GetView();
            projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(56f), (float)_constants.screenWidth / (float)_constants.screenHeight, 0.1f, 100f);

            foreach (Model renderObject in renderObjects)
            //example rendering code
            {
                foreach (ModelMesh mesh in renderObject.Meshes) //loop over each mesh in model
                {
                    foreach (BasicEffect effect in mesh.Effects) //loop over each shader on mesh
                    {
                        effect.World = world; //pass in matrices
                        effect.View = view; //..
                        effect.Projection = projection; //..
                    }

                    //mesh.Draw(); //draw to screen
                }
            }

            board.Draw(view, projection);

            base.Draw(gameTime);
        }
    }
}