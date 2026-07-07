using UnityEngine;

namespace WeaponMazeAlchemy.Game
{
    public class GameBattleView : MonoBehaviour
    {
        private const float CellSize = 1f;

        private static Sprite squareSprite;
        private BattleController battle;

        public void Bind(BattleController battleController)
        {
            battle = battleController;
            Render();
        }

        public void Render()
        {
            ClearChildren();
            if (battle == null)
            {
                return;
            }

            BuildTiles();
            DrawActor(battle.Player, new Color(0.2f, 0.75f, 1f));
            foreach (EnemyActor enemy in battle.Enemies)
            {
                if (enemy.IsAlive)
                {
                    DrawActor(enemy, new Color(1f, 0.35f, 0.35f));
                }
            }
        }

        private void BuildTiles()
        {
            for (int y = 0; y < battle.Map.Height; y++)
            {
                for (int x = 0; x < battle.Map.Width; x++)
                {
                    GridPosition position = new GridPosition(x, y);
                    bool isWall = battle.Map.IsWall(position);
                    GameObject tile = CreateSquare($"Game Tile {x},{y}", position, isWall ? 0.98f : 0.92f, GetTileColor(x, y, isWall), isWall ? 1 : 0);
                    tile.transform.SetParent(transform);
                }
            }
        }

        private void DrawActor(Actor actor, Color color)
        {
            GameObject body = CreateSquare(actor.ActorName, actor.Position, 0.72f, color, 10);
            body.transform.SetParent(transform);

            GameObject marker = CreateSquare("Facing Marker", GridPosition.Zero, 0.22f, Color.white, 11);
            marker.transform.SetParent(body.transform);
            GridPosition offset = actor.Direction.ToGridOffset();
            marker.transform.localPosition = new Vector3(offset.X * 0.55f, offset.Y * 0.55f, -0.01f);
        }

        private GameObject CreateSquare(string objectName, GridPosition position, float scale, Color color, int sortingOrder)
        {
            GameObject square = new GameObject(objectName);
            square.transform.position = position.ToWorldPosition(CellSize);
            square.transform.localScale = Vector3.one * scale;

            SpriteRenderer renderer = square.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSquareSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return square;
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }
        }

        private static Color GetTileColor(int x, int y, bool isWall)
        {
            if (isWall)
            {
                return new Color(0.08f, 0.08f, 0.09f);
            }

            return (x + y) % 2 == 0
                ? new Color(0.2f, 0.22f, 0.24f)
                : new Color(0.16f, 0.18f, 0.2f);
        }

        private static Sprite GetSquareSprite()
        {
            if (squareSprite != null)
            {
                return squareSprite;
            }

            Texture2D texture = new Texture2D(1, 1)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            squareSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return squareSprite;
        }
    }
}
