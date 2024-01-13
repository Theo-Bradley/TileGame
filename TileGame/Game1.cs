using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;


namespace TileGame
{
    public class _constants
    {
        public const int screenWidth = 1100;
        public const int screenHeight = 1000;
        public const float PI = 3.14159f;
    }

    public class GameObject
    {
        protected Vector2 position;
        protected Vector2 scale;
        protected Quaternion rotation;

        public GameObject()
        {
            position = new Vector2(0f);
            scale = new Vector2(1f);
            rotation = Quaternion.Identity;
        }

        public void Scale(Vector2 amount)
        { 
            scale += amount;
        }

        public void Rotate(float angle)
        {
            //implement
        }

        public void Move(Vector2 amount)
        { 
            position += amount;    
        }

        public void SetPosition(Vector2 _position)
        {
            position = _position;
        }

        public void SetRotation(Quaternion rotation)
        {
            // implement
        }

        public void SetScale(Vector2 _scale)
        {
            scale = _scale;
        }

        public Vector2 GetPosition()
        {
            return position;
        }
    }

    public class Piece : GameObject
    {
        public enum Direction
        {
            none,
            left,
            right,
            up,
            down,
        };
        public bool empty;
        public bool selected = false;
        public Vector2 correctPos;
        private Texture2D pieceTex;
        private Rectangle renderRect;
        private Rectangle textureRect;
        private Vector2 oldPosition;
        private Vector2 mouseOffset;
        private Direction swapDirection;

        public Piece(Texture2D loadedPieceTex, bool isEmpty, Rectangle texRect, Vector2 correctPosition) : base()
        {
            pieceTex = loadedPieceTex;
            textureRect = texRect;
            empty = isEmpty;
            UpdateRect();
            oldPosition = position;
            correctPos = correctPosition;
        }

        public void Draw(ref SpriteBatch batch, Texture2D emptyTex)
        {
            if (empty)
                batch.Draw(emptyTex, renderRect, textureRect, Color.White);
            else
                batch.Draw(pieceTex, renderRect, textureRect, Color.White);
        }

        public bool Click(Vector2 clickPos)
        {
            bool nowSelected = renderRect.Contains(clickPos);
            if (nowSelected && !selected)
                Selected(clickPos);
            if (!nowSelected && selected)
                DeSelected();
            selected = nowSelected;
            return nowSelected;
        }

        public void UpdateRect()
        {
            renderRect = new Rectangle((int) MathF.Floor(position.X),
                (int)MathF.Floor(position.Y),
                (int) MathF.Ceiling((float)textureRect.Width * scale.X),
                (int) MathF.Ceiling((float)textureRect.Height * scale.Y));
        }

        public Rectangle GetRect()
        {
            return renderRect;
        }

        public void DragPosition(Vector2 mousePosition)
        {
            Vector2 deltaX = new Vector2(mousePosition.X - oldPosition.X, 0f); //change in the x axis
            Vector2 deltaY = new Vector2(0f, mousePosition.Y - oldPosition.Y); //change in the y axis

            if (deltaX.Length() > deltaY.Length()) //if deltaX is greater than deltaY
                SetPosition(new Vector2(mousePosition.X + mouseOffset.X, oldPosition.Y - renderRect.Height/2)); //update x position to mouse pos plus offset
            else
                SetPosition(new Vector2(oldPosition.X - renderRect.Width/2, mousePosition.Y + mouseOffset.Y)); //.. y ..
        }

        public void Selected(Vector2 mousePos)
        {
            selected = true;
            oldPosition = position + new Vector2(renderRect.Width/2, renderRect.Height/2);
            mouseOffset = position - mousePos;
        }

        public Direction DeSelected()
        {
            if (selected)
            {
                selected = false;
                Vector2 deltaDirX = new Vector2(position.X + renderRect.Width/2 - oldPosition.X, 0);
                Vector2 deltaDirY = new Vector2(0, position.Y + renderRect.Height/2 - oldPosition.Y);
                SetPosition(oldPosition - new Vector2(renderRect.Width/2, renderRect.Height/2));
                if (deltaDirX.Length() == 0 && deltaDirY.Length() == 0)
                {
                    return Direction.none;
                }
                else
                {
                    if (deltaDirX.Length() >= deltaDirY.Length() && deltaDirX.X > 0)
                    {
                        return Direction.right;
                    }
                    if (deltaDirX.Length() >= deltaDirY.Length() && deltaDirX.X < 0)
                    { 
                        return Direction.left;    
                    }
                    if (deltaDirY.Length() >= deltaDirX.Length() && deltaDirY.Y > 0)
                    {
                        return Direction.down;
                    }
                    if (deltaDirY.Length() >= deltaDirX.Length() && deltaDirY.Y < 0)
                    {
                        return Direction.up;
                    }
                }
            }

            return Direction.none; //unnecessary as the code should never reach here, supresses compiler error, however in case of bug check here first
        }

        public void SetOldPosition(Vector2 position)
        {
            oldPosition = position;
        }

        #region transformations
        public void Scale(Vector2 amount, bool aboutCenter)
        {
            Vector2 originalScale = scale;
            base.Scale(amount);
            if (aboutCenter)
            {
                position -= new Vector2(textureRect.Width * (scale.X - originalScale.X)/2, textureRect.Width * (scale.Y - originalScale.Y)/2);
            }
            UpdateRect();
        }

        new public void Rotate(float angle)
        {
            //implement
            UpdateRect();
        }

        new public void Move(Vector2 amount)
        { 
            base.Move(amount);
            UpdateRect();
        }

        new public void SetPosition(Vector2 _position)
        {
            base.SetPosition(_position);
            UpdateRect();
        }

        public void SetPositionCenter(Vector2 _position)
        {
            base.SetPosition(_position - new Vector2(renderRect.Width, renderRect.Height)/2);
            UpdateRect();
        }

        new public void SetRotation(Quaternion rotation)
        {
            // implement
            UpdateRect();
        }

        new public void SetScale(Vector2 _scale)
        {

            base.SetScale(_scale);
            UpdateRect();
        }
        #endregion transformations
    }

    public class Board
    {
        public Piece[,] pieces;
        protected int size;
        public int count = 0;
        public Texture2D loadedAtlas; //(loaded) texture atlas for board
        public float boardPixels = 800f; //total width of board
        public Texture2D emptyTex; //texture for empty piece

        public Board(int boardSize, Texture2D loadedAtlasTex, GraphicsDevice graphicsDevice)
        {
            size = boardSize;
            loadedAtlas = loadedAtlasTex;
            pieces = new Piece[size, size];

            Vector2 positionOffset = Vector2.Zero; //xy shift of square image subset
            Vector2 sizeOffset = Vector2.Zero; //size offset to keep subset square
            if (loadedAtlas.Width < loadedAtlas.Height) //if taller than wide, then width is the maximum size of subset
            {
                positionOffset.Y = (float)(loadedAtlas.Height - loadedAtlas.Width) / 2;
                sizeOffset.Y = loadedAtlas.Height - loadedAtlas.Width;
            }
            if (loadedAtlas.Height < loadedAtlas.Width) //..wider than tall.. height ..
            {
                positionOffset.X = (float)(loadedAtlas.Width - loadedAtlas.Height) / 2;
                sizeOffset.X = loadedAtlas.Width - loadedAtlas.Height;
            }
            Rectangle squareRect = new Rectangle((int)MathF.Floor(positionOffset.X),
                (int)MathF.Floor(positionOffset.Y),
                (int)MathF.Floor(loadedAtlas.Width - sizeOffset.X),
                (int)MathF.Floor(loadedAtlas.Height - sizeOffset.Y)); //largest possible square subset of image

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    pieces[x, y] = new Piece(loadedAtlas, x == size - 1 && y == size - 1, new Rectangle(
                        (int)Math.Round((float)x * squareRect.Width/size + squareRect.X),
                        (int)Math.Round((float)y * squareRect.Height/size + squareRect.Y),
                        (int)Math.Round((float)squareRect.Width/size),
                        (int)Math.Round((float)squareRect.Height/size)),
                        new Vector2(x, y)); //create piece
                    pieces[x, y].SetScale(new Vector2(boardPixels / squareRect.Width,
                        boardPixels / squareRect.Height)); //scale board to size: boardPixels
                    pieces[x, y].SetPosition(new Vector2(
                        (float)Math.Ceiling((float) x * boardPixels / size),
                        (float)Math.Ceiling((float) y * boardPixels / size)));  //set inital position
                }
            }

            //create texture for empty piece
            Vector2 texSize = new Vector2((float)squareRect.Width / size, (float)squareRect.Height / size); //size of new tex
            emptyTex = new Texture2D(graphicsDevice, (int)MathF.Floor(texSize.X), (int)MathF.Floor(texSize.Y)); //init tex
             
            Color[] texData = new Color[(int)MathF.Floor(texSize.X) * (int)MathF.Floor(texSize.Y)]; //init data array
            for (int i = 0; i < texData.Length; i++) //loop over array
            {
                texData[i] = Color.CornflowerBlue; //populate with colour
            }

            emptyTex.SetData<Color>(texData); //fill tex
        }

        public void Draw(ref SpriteBatch batch)
        {
            batch.Begin();
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    pieces[x, y].Draw(ref batch, emptyTex);
                }
            }
            batch.End();
        }

        public bool IsCorrect()
        {
            bool wrong = false;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    if (pieces[x,y].correctPos.X == x && pieces[x,y].correctPos.Y == y)
                        wrong = true;
                    else
                        wrong = false;
                }
            }
            return wrong;
        }

        public bool Click(Vector2 clickPos, ref int indexX, ref int indexY)
        {
            bool result = false;
            indexX = -1;
            indexY = -1;

            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                { 
                    if (pieces[x, y].Click(clickPos))
                    {
                        indexX = x;
                        indexY = y;
                        result = true;
                    }
                }
            }
            return result;
        }

        public void Swap(int indexX, int indexY, Piece.Direction direction)
        {
            switch (direction)
            {
                case Piece.Direction.left:
                    if (indexX > 0)
                    {
                        if (pieces[indexX - 1, indexY].empty)  
                        {
                            ref Piece selected = ref pieces[indexX, indexY];
                            selected.Move(new Vector2(-selected.GetRect().Width, 0));
                            selected.SetOldPosition(selected.GetPosition());
                            Piece temp = pieces[indexX - 1, indexY];
                            pieces[indexX - 1, indexY] = pieces[indexX, indexY];
                            pieces[indexX, indexY] = temp;
                        }
                    }
                    break;
                case Piece.Direction.right:
                    if (indexX < size - 1)
                    {
                        if (pieces[indexX + 1, indexY].empty)  
                        {
                            ref Piece selected = ref pieces[indexX, indexY];
                            selected.Move(new Vector2(selected.GetRect().Width, 0));
                            selected.SetOldPosition(selected.GetPosition());
                            Piece temp = pieces[indexX + 1, indexY];
                            pieces[indexX + 1, indexY] = pieces[indexX, indexY];
                            pieces[indexX, indexY] = temp;
                        }
                    }
                    break;
                case Piece.Direction.up:
                    if (indexY > 0)
                    {
                        if (pieces[indexX, indexY - 1].empty)
                        {
                            ref Piece selected = ref pieces[indexX, indexY];
                            selected.Move(new Vector2(0, -selected.GetRect().Height));
                            selected.SetOldPosition(selected.GetPosition());
                            Piece temp = pieces[indexX, indexY - 1];
                            pieces[indexX, indexY - 1] = pieces[indexX, indexY];
                            pieces[indexX, indexY] = temp;
                        }
                    }
                    break;
                case Piece.Direction.down:
                    if (indexY < size - 1)
                    {
                        if (pieces[indexX, indexY + 1].empty)
                        {
                            ref Piece selected = ref pieces[indexX, indexY];
                            selected.Move(new Vector2(0, selected.GetRect().Height));
                            selected.SetOldPosition(selected.GetPosition());
                            Piece temp = pieces[indexX, indexY + 1];
                            pieces[indexX, indexY + 1] = pieces[indexX, indexY];
                            pieces[indexX, indexY] = temp;
                        }
                    }
                    break;
            }
        }

        public void Scramble(int n)
        {
            Random r = new Random(); //init
            Vector2 emptyPiece = new Vector2(size - 1, size - 1); //epty square index is always bottom right
            for (int i = 0; i < n; i++) //loop n times
            {
                bool swapped = false; //init
                Piece.Direction swapDirection = (Piece.Direction)r.Next(5); //pick a random direction
                switch (swapDirection)
                {
                    case Piece.Direction.right: //if moving right
                    {
                        if (emptyPiece.X == size - 1) //if on right edge
                        {
                            swapDirection = Piece.Direction.left; //swap right movement
                            swapped = true;
                        }
                        Swap((int)emptyPiece.X, (int)emptyPiece.Y, swapDirection); //actually swap pieces
                        if (swapped)
                            emptyPiece.X -= 1; //move empty piece index to the left
                        else
                            emptyPiece.X += 1; //move empty piece index to the right
                        break;
                    }
                    case Piece.Direction.left:
                    {
                        if (emptyPiece.X == 0)
                        {
                            swapDirection = Piece.Direction.right; //movement left
                            swapped = true;
                        }
                        Swap((int)emptyPiece.X, (int)emptyPiece.Y, swapDirection); //swap piece
                        if (swapped)
                            emptyPiece.X += 1; //empty to right
                        else
                            emptyPiece.X -= 1; //empty to left
                        break;
                    }
                    case Piece.Direction.down:
                    {
                        if (emptyPiece.Y == size - 1)
                        {
                            swapDirection = Piece.Direction.up; //..down
                            swapped = true;
                        }
                        Swap((int)emptyPiece.X, (int)emptyPiece.Y, swapDirection); //swap piece
                        if (swapped)
                            emptyPiece.Y -= 1; //empty up
                        else
                            emptyPiece.Y += 1; //empty down
                        break;
                    }
                    case Piece.Direction.up:
                    {
                        if (emptyPiece.Y == 0)
                        {
                            swapDirection = Piece.Direction.down; //.. up
                            swapped = true;
                        }
                        Swap((int)emptyPiece.X, (int)emptyPiece.Y, swapDirection); //swap piece
                        if (swapped)
                            emptyPiece.Y += 1; //empty down
                        else
                            emptyPiece.Y -= 1; //empty up
                        break;
                    }
                }
            }
        }
    }

    public class Button : GameObject
    {
        Rectangle renderRect;
        Texture2D textureRect;
        Color buttonColor;
        Func<int> clickFunc;

        public Button(Vector2 startPos, Vector2 extents, Color color, Func<int> ClickFunction, GraphicsDevice graphicsDevice) : base()
        {
            renderRect = new Rectangle((int)MathF.Round(startPos.X),
                (int)MathF.Round(startPos.Y),
                (int)MathF.Round(extents.X),
                (int)MathF.Round(extents.Y));
            textureRect = new Texture2D(graphicsDevice, 1, 1);
            textureRect.SetData(new Color[]{new Color(1f, 1f, 1f, 1f)}); //fill texture with a white colour
            buttonColor = color;
            clickFunc = ClickFunction;
        }

        public void Draw(ref SpriteBatch batch)
        {
            batch.Draw(textureRect, renderRect, buttonColor);
        }

        public void Clicked(Vector2 mPos)
        {
            if (renderRect.Contains(mPos)) //if mouse was inside the render rect when clicked
            {
                int result = clickFunc(); //call clickFunc (result is unused)
            }
        }
    }

    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        float deltaTime = 0;
        Vector2 mousePos;

        private Texture2D customAtlas; //custom loaded atlas
        private Texture2D giraffeAtlas;
        private Board board;
        private int indexX = 0, indexY = 0;
        private bool wasDown = false;
        private int bin = 0; //used to hold discarded arguments
        private Button scrambleButton;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }
        private int ScrambleBoard()
        {
            board.Scramble(1000000); //scramble board

            return 0; //ignore this
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();

            _graphics.PreferredBackBufferWidth = _constants.screenWidth;
            _graphics.PreferredBackBufferHeight = _constants.screenHeight;
            _graphics.ApplyChanges();

            scrambleButton = new Button(new Vector2(850, 150), new Vector2(100, 100), new Color(47, 54, 61), ScrambleBoard, _graphics.GraphicsDevice);

            board = new Board(3, giraffeAtlas, _graphics.GraphicsDevice); //init new board
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            giraffeAtlas = Content.Load<Texture2D>("giraffe");
            customAtlas = Texture2D.FromFile(_graphics.GraphicsDevice,
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Content/customAtlas.png");
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            MouseState mState = Mouse.GetState(); //get mouse state
            mousePos.X = mState.Position.X; //update mouse position
            mousePos.Y = mState.Position.Y; //..

            Window.Title = board.IsCorrect().ToString();

            if (mState.LeftButton == ButtonState.Pressed) //if clicking and clicking a piece
            {
                if (!wasDown) //if wasn't clicking on last frame
                {
                    board.Click(mousePos, ref indexX, ref indexY); //select piece
                    scrambleButton.Clicked(mousePos);
                }
                if (indexX >= 0 && indexY >= 0) //if clicked on a piece
                    board.pieces[indexX, indexY].DragPosition(mousePos); //update position

                wasDown = true; //indicate the mouse was clicked on next loop
            }

            if (mState.LeftButton == ButtonState.Released)
            {
                if (wasDown) //if was clicking on last frame
                    if (indexX >= 0 && indexY >= 0) //if clicking on a tile
                    {
                        Piece.Direction direction = board.pieces[indexX, indexY].DeSelected(); //unselect piece
                        if (direction != Piece.Direction.none) //if piece wasn't released in the same spot
                        {
                            board.Swap(indexX, indexY, direction); //swap
                        }
                    }
                wasDown = false; //reset mouse flipflop
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                board = new Board(3, customAtlas, _graphics.GraphicsDevice);
            }

            base.Update(gameTime);
            deltaTime = (float) gameTime.ElapsedGameTime.TotalSeconds; //calculate delta time in seconds
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(36, 41, 46)); //clear with dark grey

            board.Draw(ref _spriteBatch);
            
            _spriteBatch.Begin();
            scrambleButton.Draw(ref _spriteBatch);
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}