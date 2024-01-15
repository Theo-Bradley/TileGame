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
            renderRect = new Rectangle((int)Math.Round(position.X),
                (int)Math.Round(position.Y),
                (int)Math.Round(textureRect.Width * scale.X),
                (int)Math.Round(textureRect.Height * scale.Y));
        }

        public Rectangle GetRect()
        {
            return renderRect;
        }

        public void DragPosition(Vector2 mousePosition)
        {
            if (empty)
                return;
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
        public int size;
        public int count = 0;
        public Texture2D loadedAtlas; //(loaded) texture atlas for board
        public float boardPixels = 800f; //total width of board
        public Texture2D emptyTex; //texture for empty piece
        public Vector2 emptyPos;

        public Board(Vector2 position, int boardSize, Texture2D loadedAtlasTex, GraphicsDevice graphicsDevice)
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
            Rectangle squareRect = new Rectangle((int)MathF.Round(positionOffset.X),
                (int)MathF.Round(positionOffset.Y),
                (int)MathF.Round(loadedAtlas.Width - sizeOffset.X),
                (int)MathF.Round(loadedAtlas.Height - sizeOffset.Y)); //largest possible square subset of image

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
                        (float)Math.Round(x * boardPixels / size + position.X),
                        (float)Math.Round(y * boardPixels / size + position.Y)));  //set inital position
                    if (x != 0)
                    {
                        if (pieces[x, y].GetPosition().X > pieces[x - 1, y].GetPosition().X + pieces[x - 1, y].GetRect().Width)
                        {
                            pieces[x, y].SetPosition(new Vector2(pieces[x - 1, y].GetPosition().X + pieces[x - 1, y].GetRect().Width,
                                pieces[x, y].GetPosition().Y));
                        }
                    }
                    if (y != 0)
                    {
                        if (pieces[x, y].GetPosition().Y > pieces[x, y - 1].GetPosition().Y + pieces[x, y - 1].GetRect().Height)
                        {
                            pieces[x, y].SetPosition(new Vector2(pieces[x, y].GetPosition().X,
                                pieces[x, y - 1].GetPosition().Y + pieces[x, y - 1].GetRect().Height));
                        }
                    }
                }
            }

            emptyPos = new Vector2(size - 1, size - 1); //update the empty position

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
                    if (pieces[x, y].empty)
                    {
                        pieces[x, y].Draw(ref batch, emptyTex);
                        goto BoardDrawJump;
                    }
                }
            }
            BoardDrawJump:
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    if (!pieces[x, y].empty)
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

        public bool Swap(int indexX, int indexY, Piece.Direction direction)
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
                            temp.Move(new Vector2(temp.GetRect().Width, 0));
                            temp.SetOldPosition(temp.GetPosition());
                            pieces[indexX - 1, indexY] = pieces[indexX, indexY];
                            pieces[indexX, indexY] = temp;
                            return true;
                        }
                        return false;
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
                            temp.Move(new Vector2(-temp.GetRect().Width, 0));
                            temp.SetOldPosition(temp.GetPosition());
                            pieces[indexX + 1, indexY] = pieces[indexX, indexY];
                            pieces[indexX, indexY] = temp;
                            return true;
                        }
                        return false;
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
                            temp.Move(new Vector2(0, temp.GetRect().Height));
                            temp.SetOldPosition(temp.GetPosition());
                            pieces[indexX, indexY - 1] = pieces[indexX, indexY];
                            pieces[indexX, indexY] = temp;
                            return true;
                        }
                        return false;
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
                            temp.Move(new Vector2(0, -temp.GetRect().Height));
                            temp.SetOldPosition(temp.GetPosition());
                            pieces[indexX, indexY + 1] = pieces[indexX, indexY];
                            pieces[indexX, indexY] = temp;
                            return true;
                        }
                        return false;
                    }
                    break;
            }
            return false;
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
                        if (swapped)
                        {
                            Swap((int)emptyPiece.X - 1, (int)emptyPiece.Y, Piece.Direction.right); //actually swap pieces
                            emptyPiece.X -= 1; //move empty piece index to the left
                        }
                        else
                        {
                            Swap((int)emptyPiece.X + 1, (int)emptyPiece.Y, Piece.Direction.left); //actually swap pieces
                            emptyPiece.X += 1; //move empty piece index to the right
                        }
                        break;
                    }
                    case Piece.Direction.left:
                    {
                        if (emptyPiece.X == 0)
                        {
                            swapDirection = Piece.Direction.right; //movement left
                            swapped = true;
                        }
                        if (swapped)
                        {
                            Swap((int)emptyPiece.X + 1, (int)emptyPiece.Y, Piece.Direction.left); //actually swap pieces
                            emptyPiece.X += 1; //empty to right
                        }
                        else
                        {
                            Swap((int)emptyPiece.X - 1, (int)emptyPiece.Y, Piece.Direction.right); //actually swap pieces
                            emptyPiece.X -= 1; //empty to left
                        }
                        break;
                    }
                    case Piece.Direction.down:
                    {
                        if (emptyPiece.Y == size - 1)
                        {
                            swapDirection = Piece.Direction.up; //..up
                            swapped = true;
                        }
                        if (swapped)
                        {
                            Swap((int)emptyPiece.X, (int)emptyPiece.Y - 1, Piece.Direction.down); //actually swap pieces
                            emptyPiece.Y -= 1; //empty up
                        }
                        else
                        {
                            Swap((int)emptyPiece.X, (int)emptyPiece.Y + 1, Piece.Direction.up); //actually swap pieces
                            emptyPiece.Y += 1; //empty down
                        }
                        break;
                    }
                    case Piece.Direction.up:
                    {
                        if (emptyPiece.Y == 0)
                        {
                            swapDirection = Piece.Direction.down; //.. up
                            swapped = true;
                        }
                        if (swapped)
                        {
                            Swap((int)emptyPiece.X, (int)emptyPiece.Y + 1, Piece.Direction.up); //actually swap pieces
                            emptyPiece.Y += 1; //empty down
                        }
                        else
                        {
                            Swap((int)emptyPiece.X, (int)emptyPiece.Y - 1, Piece.Direction.down); //actually swap pieces
                            emptyPiece.Y -= 1; //empty up
                        }
                        break;
                    }
                }
            }

            emptyPos = emptyPiece; //update the position of the empty piece in the class
        }
    }

    public class Button : GameObject
    {
        Rectangle renderRect;
        Rectangle iconRect;
        Texture2D textureRect;
        Texture2D iconTex;
        Color buttonColor;
        Func<int> clickFunc;

        public Button(Vector2 startPos, Vector2 extents,
            Color color, Texture2D loadedIcon,
            Func<int> ClickFunction, GraphicsDevice graphicsDevice) : base()
        {
            renderRect = new Rectangle((int)MathF.Round(startPos.X),
                (int)MathF.Round(startPos.Y),
                (int)MathF.Round(extents.X),
                (int)MathF.Round(extents.Y));
            iconRect = new Rectangle((int)MathF.Round(extents.X * 0.1f + startPos.X),
                (int)MathF.Round(extents.Y * 0.1f + startPos.Y),
                (int)MathF.Round(extents.X * 0.8f),
                (int)MathF.Round(extents.Y * 0.8f));
            textureRect = new Texture2D(graphicsDevice, 1, 1); //init texture
            textureRect.SetData(new Color[]{new Color(1f, 1f, 1f, 1f)}); //fill texture with a white colour
            iconTex = loadedIcon;
            buttonColor = color;
            clickFunc = ClickFunction;
        }

        public void Draw(ref SpriteBatch batch)
        {
            batch.Draw(textureRect, renderRect, buttonColor);
            batch.Draw(iconTex, iconRect, Color.White);
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
        private Texture2D refresh;
        private Texture2D img;
        private Texture2D sMinus;
        private Texture2D sPlus;
        private Board board;
        private int indexX = 0, indexY = 0;
        private Vector2 piecePos;
        private bool wasDown = false;
        private bool usingCustomAtlas = false;
        private int bin = 0; //used to hold discarded arguments
        private byte arrowKeysFlag = 0b00000000; //holds which arrow keys were pressed on the last frame
        private Button scrambleButton;
        private Button imageButton;
        private Button sizePlusButton;
        private Button sizeMinusButton;

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

        private int SizeMinus()
        {
            if (board.size < 2)
                return 0; //early exit
            //regen smaller board
            board = new Board(new Vector2(25, 25), board.size - 1, board.loadedAtlas, _graphics.GraphicsDevice);
            return 0; //..
        }

        private int SizePlus()
        {
            if (board.size > 7)
                return 0; //early exit
            //regen larger board
            board = new Board(new Vector2(25, 25), board.size + 1, board.loadedAtlas, _graphics.GraphicsDevice);
            return 0; //..
        }

        private int SwapAtlas()
        {
            if (usingCustomAtlas)
            {
                board = new Board(new Vector2(25, 25), board.size, giraffeAtlas, _graphics.GraphicsDevice); //regenrate board with atlas
                usingCustomAtlas = false;
            }
            else
            {
                board = new Board(new Vector2(25, 25), board.size, customAtlas, _graphics.GraphicsDevice); //regenrate board with atlas
                usingCustomAtlas = true;
            }

            return 0;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            giraffeAtlas = Content.Load<Texture2D>("giraffe");
            refresh = Content.Load<Texture2D>("refresh-cw(1)");
            img = Content.Load<Texture2D>("image(1)");
            sMinus = Content.Load<Texture2D>("minus");
            sPlus = Content.Load<Texture2D>("plus");
            customAtlas = Texture2D.FromFile(_graphics.GraphicsDevice,
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "/Content/customAtlas.png"); //load custom atlas
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();

            _graphics.PreferredBackBufferWidth = _constants.screenWidth; //resize buffers to window size
            _graphics.PreferredBackBufferHeight = _constants.screenHeight; //..
            _graphics.ApplyChanges(); //update

            scrambleButton = new Button(new Vector2(850, 150), new Vector2(100, 100),
                new Color(47, 54, 61), refresh,
                ScrambleBoard, _graphics.GraphicsDevice); //create button to scramble board

            imageButton = new Button(new Vector2(975, 150), new Vector2(100, 100),
                new Color(47, 54, 61), img,
                SwapAtlas, _graphics.GraphicsDevice); //create an image button

            sizeMinusButton = new Button(new Vector2(850, 275), new Vector2(100, 100),
                new Color(47, 54, 61), sMinus,
                SizeMinus, _graphics.GraphicsDevice); //create a minus button

            sizePlusButton = new Button(new Vector2(975, 275), new Vector2(100, 100),
                new Color(47, 54, 61), sPlus,
                SizePlus, _graphics.GraphicsDevice); //create a plus button

            board = new Board(new Vector2(25, 25), 4, giraffeAtlas, _graphics.GraphicsDevice); //init new board
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
                    scrambleButton.Clicked(mousePos); //tell button a click has occured
                    imageButton.Clicked(mousePos); //..
                    sizeMinusButton.Clicked(mousePos); //..
                    sizePlusButton.Clicked(mousePos); //..
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

            if (Keyboard.GetState().IsKeyDown(Keys.Left))
            {
                if ((arrowKeysFlag & 0b00001000) == 0b00000000) //if left arrow not pressed on last frame
                {
                    piecePos = board.emptyPos; //get position of empty piece
                    if (piecePos.X < board.size - 1) //if not on edge
                    {
                        if (board.Swap((int)piecePos.X + 1, (int)piecePos.Y, Piece.Direction.left)) //move piece to the right left
                            board.emptyPos.X += 1; //update empty pos if actually moved
                    }
                    arrowKeysFlag |= 0b00001000; //set left arrow as pressed last frame
                }
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Right))
            {
                if ((arrowKeysFlag & 0b00000100) == 0b00000000) //.. right ..
                {
                    piecePos = board.emptyPos; //..
                    if (piecePos.X > 0) //..
                    {
                        if(board.Swap((int)piecePos.X - 1, (int)piecePos.Y, Piece.Direction.right)) //..
                            board.emptyPos.X -= 1; //..
                    }
                    arrowKeysFlag |= 0b00000100; //.. right ..
                }
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Up))
            {
                if ((arrowKeysFlag & 0b00000010) == 0b00000000) //.. up ..
                {
                    piecePos = board.emptyPos; //..
                    if (piecePos.Y < board.size - 1) //..
                    {
                        if (board.Swap((int)piecePos.X, (int)piecePos.Y + 1, Piece.Direction.up)) //..
                            board.emptyPos.Y += 1; //..
                    }
                    arrowKeysFlag |= 0b00000010; //.. up ..
                }
            }
            if (Keyboard.GetState().IsKeyDown(Keys.Down))
            {
                if ((arrowKeysFlag & 0b00000001) == 0b00000000) //.. down ..
                {
                    piecePos = board.emptyPos; //..
                    if (piecePos.Y > 0) //..
                    {
                        if (board.Swap((int)piecePos.X, (int)piecePos.Y - 1, Piece.Direction.down)) //..
                        {
                            board.emptyPos.Y -= 1; //..
                        }
                    }
                    arrowKeysFlag |= 0b00000001; //.. down ..
                }
            }

            if (Keyboard.GetState().IsKeyUp(Keys.Left))
                arrowKeysFlag &= 0b11110111; //unset flag
            if (Keyboard.GetState().IsKeyUp(Keys.Right))
                arrowKeysFlag &= 0b11111011; //..
            if (Keyboard.GetState().IsKeyUp(Keys.Up))
                arrowKeysFlag &= 0b11111101; //..
            if (Keyboard.GetState().IsKeyUp(Keys.Down))
                arrowKeysFlag &= 0b11111110; //..

            base.Update(gameTime);
            deltaTime = (float) gameTime.ElapsedGameTime.TotalSeconds; //calculate delta time in seconds
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(new Color(36, 41, 46)); //clear with dark grey

            board.Draw(ref _spriteBatch);
            
            _spriteBatch.Begin();
            scrambleButton.Draw(ref _spriteBatch); //draw buttons
            imageButton.Draw(ref _spriteBatch); //..
            sizeMinusButton.Draw(ref _spriteBatch); //..
            sizePlusButton.Draw(ref _spriteBatch); //..
            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}