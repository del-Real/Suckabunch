using System;
using Raylib_cs;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Sokoban;

public class Renderer {

    private const int TILE_SIZE = 24;
    private int screenWidth;
    private int screenHeight;
    private int offset;

    private Texture2D floorTexture;
    private Texture2D targetTexture;
    private Texture2D boxTexture;
    private Texture2D wallTexture;

    private Texture2D playerIdleTexture;
    private Texture2D playerUpTexture;
    private Texture2D playerRightTexture;
    private Texture2D playerDownTexture;
    private Texture2D playerLeftTexture;

    private Texture2D playerPushUpTexture;
    private Texture2D playerPushRightTexture;
    private Texture2D playerPushDownTexture;
    private Texture2D playerPushLeftTexture;

    private Level level;
    private State state;

    private Camera2D camera;

    /*
        // Renderer constructor
        public Renderer() {
            screenWidth = 1280;     // screen width set by tile size
            screenHeight = 720;    // screen height set by tile size
            offset = TILE_SIZE * 2;                                 // offset to add margins

            LaunchMenu();
        }
    */

    // Renderer constructor
    public Renderer(Level level, State state) {
        this.level = level;
        this.state = state;

        // Calculate screen dimensions based on level size
        int cols = level.Cols;
        int rows = level.Rows;

        screenWidth = (TILE_SIZE * cols) + (TILE_SIZE * 16);     // screen width set by tile size
        screenHeight = (TILE_SIZE * rows) + (TILE_SIZE * 16);    // screen height set by tile size
        offset = TILE_SIZE * 8;                                 // offset to add margins

        camera = new Camera2D {
            Target = new Vector2(screenWidth / 2, screenHeight / 2),    // camera target (center of the screen)
            Offset = new Vector2(screenWidth / 2, screenHeight / 2),    // camera offset (center of the screen)
            Rotation = 0.0f,                                            // no rotation
            Zoom = 2.0f,                                                // initial zoom
        };
    }

    // Initializes the game window, sets up the frame rate, and loads the necessary textures for the game elements
    private void Initialize() {

        Raylib.SetTraceLogLevel(TraceLogLevel.None);                 // disable Raylib log info
        Raylib.InitWindow(screenWidth, screenHeight, "Soakoban");    // window initilizer
        Raylib.SetTargetFPS(60);                                     // set frame rate

        floorTexture = Raylib.LoadTexture("resources/floor.png");     // load floor texture
        targetTexture = Raylib.LoadTexture("resources/target.png");   // load target texture
        boxTexture = Raylib.LoadTexture("resources/box.png");         // load box texture
        wallTexture = Raylib.LoadTexture("resources/wall.png");       // load wall texture

        // load player textures
        playerIdleTexture = Raylib.LoadTexture("resources/wh_worker_idle.png");
        playerUpTexture = Raylib.LoadTexture("resources/wh_worker_up.png");
        playerRightTexture = Raylib.LoadTexture("resources/wh_worker_right.png");
        playerDownTexture = Raylib.LoadTexture("resources/wh_worker_down.png");
        playerLeftTexture = Raylib.LoadTexture("resources/wh_worker_left.png");

        playerPushUpTexture = Raylib.LoadTexture("resources/wh_worker_up_push.png");
        playerPushRightTexture = Raylib.LoadTexture("resources/wh_worker_right_push.png");
        playerPushDownTexture = Raylib.LoadTexture("resources/wh_worker_down_push.png");
        playerPushLeftTexture = Raylib.LoadTexture("resources/wh_worker_left_push.png");
    }

    // Manages the game loop, including drawing the game elements on the screen and handling window events
    public void Render(List<Node> nodeSolution) {
        Initialize();

        float duration = 0.35f;    // Duration for each movement
        float timeElapsed = 0.0f; // Tracks time for interpolation

        int currentFrame = 0;
        int framesCounter = 0;
        int framesSpeed = 6;

        int currentNodeIndex = 0;

        // Player movement
        (float, float) startPlayerPos = (0f, 0f);
        (float, float) endPlayerPos = (0f, 0f);
        (float, float) playerMove = (0f, 0f);
        string playerAction;

        // Boxes movement
        float[][] startBoxesPos = new float[0][];
        float[][] endBoxesPos = new float[0][];
        float[][] boxMoves = new float[0][];

        Raylib.SetTargetFPS(60);

        // Render loop
        while (!Raylib.WindowShouldClose()) {
            float dt = Raylib.GetFrameTime(); // Delta time

            // Lerp
            if (currentNodeIndex < nodeSolution.Count - 1) {

                // Set start and end positions for the current node transition
                startPlayerPos = (nodeSolution[currentNodeIndex].StateNode.Player.Item1,
                                  nodeSolution[currentNodeIndex].StateNode.Player.Item2);
                endPlayerPos = (nodeSolution[currentNodeIndex + 1].StateNode.Player.Item1,
                                nodeSolution[currentNodeIndex + 1].StateNode.Player.Item2);

                startBoxesPos = nodeSolution[currentNodeIndex].StateNode.Boxes
                    .Select(row => row.Select(col => (float)col).ToArray())
                    .ToArray();

                endBoxesPos = nodeSolution[currentNodeIndex + 1].StateNode.Boxes
                    .Select(row => row.Select(col => (float)col).ToArray())
                    .ToArray();

                // Update interpolation progress
                if (timeElapsed < duration) {
                    float t = timeElapsed / duration; // Normalized time (0 to 1)
                    playerMove = LerpPlayer(startPlayerPos, endPlayerPos, t);
                    boxMoves = LerpBoxes(startBoxesPos, endBoxesPos, t);
                    timeElapsed += dt;
                }
                else {
                    playerMove = endPlayerPos;
                    boxMoves = endBoxesPos;
                    timeElapsed = 0.0f;
                    currentNodeIndex++;
                }
            }

            // Player animation
            framesCounter++;
            if (framesCounter >= (60 / framesSpeed))    // Controls animation speed
            {
                framesCounter = 0;                      // Reset counter
                currentFrame = (currentFrame + 1) % 2;  // Toggle between 0 and 1
            }

            Rectangle playerFrame = new Rectangle(
                currentFrame * ((float)playerUpTexture.Width / 2),  // Frame X position
                0.0f,                                               // Frame Y position
                (float)playerUpTexture.Width / 2,                   // Frame width
                (float)playerUpTexture.Height                       // Frame height
            );

            // Drawing
            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.White);

            Raylib.BeginMode2D(camera);

            DrawFloor();
            //DrawGrid();
            DrawTargets();
            DrawBoxes(boxMoves);

            if (currentNodeIndex + 1 < nodeSolution.Count) {
                playerAction = nodeSolution[currentNodeIndex + 1]?.Action;
            }
            else {
                playerAction = "idle";
            }

            DrawPlayer(playerMove, playerAction, playerFrame);
            DrawWalls();

            Raylib.EndDrawing();
        }
        Cleanup();
    }

    // Lerp from start to end
    private static (float, float) LerpPlayer((float, float) startPos, (float, float) endPos, float amount) {
        float x = startPos.Item1 + (endPos.Item1 - startPos.Item1) * amount;
        float y = startPos.Item2 + (endPos.Item2 - startPos.Item2) * amount;
        return (x, y);
    }

    private static float[][] LerpBoxes(float[][] startPos, float[][] endPos, float amount) {
        // Initialize a new float array for the interpolated positions
        float[][] boxMoves = new float[startPos.Length][];

        for (int i = 0; i < startPos.Length; i++) {
            boxMoves[i] = new float[2]; // Each box position is a 2-element array (x, y)

            boxMoves[i][0] = startPos[i][0] + (endPos[i][0] - startPos[i][0]) * amount; // Interpolating x
            boxMoves[i][1] = startPos[i][1] + (endPos[i][1] - startPos[i][1]) * amount; // Interpolating y
        }

        return boxMoves;
    }

    /*
        public void LaunchMenu() {
            Initialize();

            Color lightWhite = new Color(255, 255, 255, 32); // Set grid color

            while (!Raylib.WindowShouldClose()) {

                float deltaTime = Raylib.GetFrameTime();

                Raylib.BeginDrawing();
                Raylib.ClearBackground(Color.Black);

                Raylib.EndDrawing();
            }

            Cleanup();
        }
    */

    // Draws a grid on the screen to represent the game board, using a specified color
    private void DrawFloor() {

        for (int i = 0; i <= screenWidth / TILE_SIZE; i++) {
            for (int j = 0; j <= screenHeight / TILE_SIZE; j++) {
                Raylib.DrawTexture(floorTexture, i * TILE_SIZE, j * TILE_SIZE, Color.White);
            }
        }
    }

    // Draws a grid (Debug)
    private void DrawGrid() {

        Color lightWhite = new Color(0, 0, 0, 30);

        // Draw vertical grid lines
        for (int i = 0; i < screenWidth / TILE_SIZE + 1; i++) {
            Raylib.DrawLineV(new Vector2(TILE_SIZE * i, 0), new Vector2(TILE_SIZE * i, screenHeight), lightWhite);
        }

        // Draw horizontal grid lines
        for (int i = 0; i < screenHeight / TILE_SIZE + 1; i++) {
            Raylib.DrawLineV(new Vector2(0, TILE_SIZE * i), new Vector2(screenWidth, TILE_SIZE * i), lightWhite);
        }
    }

    private Dictionary<(int, int), int> wallSprites = new Dictionary<(int, int), int>();

    private void DrawWalls() {
        Random random = new Random();

        foreach (var wall in level.Walls) {
            int x = wall[1] * TILE_SIZE + offset;
            int y = wall[0] * TILE_SIZE + offset;

            Vector2 wallPos = new Vector2(x, y);

            // Check if the wall already has a sprite assigned, if not assign one
            if (!wallSprites.ContainsKey((x, y))) {
                wallSprites[(x, y)] = random.Next(0, 4);
            }

            int randomFrame = wallSprites[(x, y)];

            Rectangle wallFrame = new Rectangle(
                randomFrame * (float)wallTexture.Width / 4,  // Frame X position
                0.0f,                                        // Frame Y position
                (float)wallTexture.Width / 4,                // Frame width
                (float)wallTexture.Height                    // Frame height
            );

            Raylib.DrawTextureRec(wallTexture, wallFrame, wallPos, Color.White);
        }
    }

    // Draws the target locations on the game board at the specified positions.
    private void DrawTargets() {
        foreach (var target in level.Targets) {
            int x = target[1];
            int y = target[0];
            Raylib.DrawTexture(targetTexture, x * TILE_SIZE + offset, y * TILE_SIZE + offset, Color.White);
        }
    }

    // Draws the boxes on the game board based on their current positions
    private void DrawBoxes(float[][] boxesCoords) {
        for (int i = 0; i < boxesCoords.Length; i++) {
            int x = (int)(boxesCoords[i][1] * TILE_SIZE + offset); // Y-axis in grid space
            int y = (int)(boxesCoords[i][0] * TILE_SIZE + offset); // X-axis in grid space
            Raylib.DrawTexture(boxTexture, x, y, Color.White);
        }
    }

    // Draws the player on the game board based on the player's current position
    private void DrawPlayer((float, float) playerCoords, string action, Rectangle playerFrame) {
        int x = (int)(playerCoords.Item2 * TILE_SIZE + offset);
        int y = (int)(playerCoords.Item1 * TILE_SIZE + offset);

        Vector2 playerPos = new Vector2(x, y);

        switch (action) {
            case "U":
                playerPos.Y -= 5;
                Raylib.DrawTextureRec(playerPushUpTexture, playerFrame, playerPos, Color.White);
                break;
            case "R":
                playerPos.X += 12;
                Raylib.DrawTextureRec(playerPushRightTexture, playerFrame, playerPos, Color.White);
                break;
            case "D":
                playerPos.Y += 5;
                Raylib.DrawTextureRec(playerPushDownTexture, playerFrame, playerPos, Color.White);
                break;
            case "L":
                playerPos.X -= 12;
                Raylib.DrawTextureRec(playerPushLeftTexture, playerFrame, playerPos, Color.White);
                break;
            case "u":
                Raylib.DrawTextureRec(playerUpTexture, playerFrame, playerPos, Color.White);
                break;
            case "r":
                Raylib.DrawTextureRec(playerRightTexture, playerFrame, playerPos, Color.White);
                break;
            case "d":
                Raylib.DrawTextureRec(playerDownTexture, playerFrame, playerPos, Color.White);
                break;
            case "l":
                Raylib.DrawTextureRec(playerLeftTexture, playerFrame, playerPos, Color.White);
                break;
            case "idle":
                Raylib.DrawTexture(playerIdleTexture, x, y, Color.White);
                break;
        }
    }

    // Releases resources by unloading textures and closing the game window.
    private void Cleanup() {
        // Unload textures and close window
        Raylib.UnloadTexture(floorTexture);
        Raylib.UnloadTexture(wallTexture);
        Raylib.UnloadTexture(targetTexture);
        Raylib.UnloadTexture(boxTexture);
        Raylib.UnloadTexture(playerUpTexture);
        Raylib.UnloadTexture(playerRightTexture);
        Raylib.UnloadTexture(playerDownTexture);
        Raylib.UnloadTexture(playerLeftTexture);
        Raylib.UnloadTexture(playerIdleTexture);
        Raylib.UnloadTexture(playerPushUpTexture);
        Raylib.UnloadTexture(playerPushRightTexture);
        Raylib.UnloadTexture(playerPushDownTexture);
        Raylib.UnloadTexture(playerPushLeftTexture);
        Raylib.CloseWindow();
    }

    // Print coords (debug)
    static void PrintCoordsArray(int[][] array) {
        Console.Write("[");

        for (int i = 0; i < array.Length; i++) {
            Console.Write("(" + array[i][0] + "," + array[i][1] + ")");

            if (i < array.Length - 1) {
                Console.Write(",");
            }
        }

        Console.WriteLine("]");
    }
}
