using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WeaponMazeAlchemy.Prototype
{
    public class PrototypeBattleView : MonoBehaviour
    {
        private const float CellSize = 1f;

        private static Sprite squareSprite;

        private readonly Dictionary<Actor, GameObject> actorObjects = new Dictionary<Actor, GameObject>();
        private readonly Dictionary<WeaponDrop, GameObject> weaponDropObjects = new Dictionary<WeaponDrop, GameObject>();
        private BattleController battle;

        public void Bind(BattleController battle)
        {
            this.battle = battle;
            actorObjects.Clear();
            weaponDropObjects.Clear();

            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Destroy(transform.GetChild(i).gameObject);
            }

            BuildGrid();
            Render();
        }

        public void Render()
        {
            if (battle == null)
            {
                return;
            }

            foreach (Actor actor in battle.Map.Actors)
            {
                if (!actorObjects.TryGetValue(actor, out GameObject actorObject))
                {
                    actorObject = CreateActorObject(actor);
                    actorObjects.Add(actor, actorObject);
                }

                UpdateActorObject(actorObject, actor);
            }

            foreach (KeyValuePair<Actor, GameObject> pair in actorObjects)
            {
                if (!battle.Map.Actors.Contains(pair.Key))
                {
                    pair.Value.SetActive(false);
                }
            }

            foreach (WeaponDrop weaponDrop in battle.WeaponDrops)
            {
                if (!weaponDropObjects.TryGetValue(weaponDrop, out GameObject dropObject))
                {
                    dropObject = CreateWeaponDropObject(weaponDrop);
                    weaponDropObjects.Add(weaponDrop, dropObject);
                }

                UpdateWeaponDropObject(dropObject, weaponDrop);
            }

            foreach (KeyValuePair<WeaponDrop, GameObject> pair in weaponDropObjects)
            {
                if (!battle.WeaponDrops.Contains(pair.Key))
                {
                    pair.Value.SetActive(false);
                }
            }
        }

        private void BuildGrid()
        {
            for (int y = 0; y < battle.Map.Height; y++)
            {
                for (int x = 0; x < battle.Map.Width; x++)
                {
                    GameObject tile = new GameObject($"Tile {x},{y}");
                    tile.transform.SetParent(transform);
                    tile.transform.position = new GridPosition(x, y).ToWorldPosition(CellSize);
                    GridPosition position = new GridPosition(x, y);
                    bool isWall = battle.Map.IsWall(position);
                    tile.transform.localScale = Vector3.one * (isWall ? 0.98f : 0.95f);

                    SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                    renderer.sprite = GetSquareSprite();
                    renderer.color = isWall
                        ? new Color(0.04f, 0.045f, 0.05f)
                        : (x + y) % 2 == 0
                            ? new Color(0.16f, 0.18f, 0.2f)
                            : new Color(0.12f, 0.14f, 0.16f);
                    renderer.sortingOrder = isWall ? 1 : 0;
                }
            }
        }

        private GameObject CreateActorObject(Actor actor)
        {
            GameObject actorObject = new GameObject(actor.ActorName);
            actorObject.transform.SetParent(transform);
            actorObject.transform.localScale = Vector3.one * 0.75f;

            SpriteRenderer body = actorObject.AddComponent<SpriteRenderer>();
            body.sprite = GetSquareSprite();
            body.color = actor.Faction == Faction.Player
                ? new Color(0.2f, 0.8f, 1f)
                : new Color(1f, 0.25f, 0.25f);
            body.sortingOrder = 10;

            GameObject marker = new GameObject("Facing Marker");
            marker.transform.SetParent(actorObject.transform);
            marker.transform.localScale = new Vector3(0.25f, 0.25f, 1f);

            SpriteRenderer markerRenderer = marker.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = GetSquareSprite();
            markerRenderer.color = Color.white;
            markerRenderer.sortingOrder = 11;

            return actorObject;
        }

        private GameObject CreateWeaponDropObject(WeaponDrop weaponDrop)
        {
            GameObject dropObject = new GameObject($"Weapon Drop Rank {weaponDrop.Weapon.Rank}");
            dropObject.transform.SetParent(transform);
            dropObject.transform.localScale = Vector3.one * 0.45f;

            SpriteRenderer renderer = dropObject.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSquareSprite();
            renderer.color = new Color(1f, 0.85f, 0.15f);
            renderer.sortingOrder = 5;

            return dropObject;
        }

        private void UpdateActorObject(GameObject actorObject, Actor actor)
        {
            actorObject.SetActive(actor.IsAlive);
            actorObject.transform.position = actor.Position.ToWorldPosition(CellSize);

            Transform marker = actorObject.transform.Find("Facing Marker");
            if (marker != null)
            {
                GridPosition offset = actor.Direction.ToGridOffset();
                marker.localPosition = new Vector3(offset.X * 0.55f, offset.Y * 0.55f, -0.01f);
            }
        }

        private void UpdateWeaponDropObject(GameObject dropObject, WeaponDrop weaponDrop)
        {
            dropObject.SetActive(true);
            dropObject.transform.position = weaponDrop.Position.ToWorldPosition(CellSize) + new Vector3(0f, 0f, -0.02f);
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
