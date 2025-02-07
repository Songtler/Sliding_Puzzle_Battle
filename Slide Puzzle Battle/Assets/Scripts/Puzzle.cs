﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

public class Puzzle : MonoBehaviour
{
    private const float TILE_CREATE_DELAY = 0.3f;
    private const float TILE_SPACING = 0f;
    private const float BOARD_WIDTH = 1000f;
    private const float TILE_SPRITE_WIDTH = 200f;

    private Vector2 startPos;
    private float tileSize;
    private int blankIndex;

    public List<TileController> tiles;
    private StageData stage;
    private TileFactory factory;
    private int currentMonsterCount;
    public int currentAttackLimit;

    public TileController tilePrefab;
    public bool createComplete;
    public bool isMoved;
    public bool isAttack;
    public bool isAttackLimit;

    private IEnumerator attackCoroutine;

    public IEnumerator Initialized(StageData _stage)
    {
        tiles = new List<TileController>();
        stage = _stage;
        tileSize = (BOARD_WIDTH / _stage.BoardSize) / TILE_SPRITE_WIDTH;
        startPos = new Vector2
        (
             -tileSize * (_stage.BoardSize - 1)
            , tileSize * (_stage.BoardSize - 1)
        );
        createComplete = false;

        factory = TileFactory.Instance;
        factory.Init(tileSize);

        currentMonsterCount = _stage.MonsterCount;

        CreateTiles(_stage);
        yield return StartCoroutine(Shuffle("Tile_Create"));

        blankIndex = tiles.Count;

        GameManager.Instance.currentPuzzle = this;

        yield return null;
    }

    private void CreateTiles(StageData _stage)
    {
        int tileCount = (_stage.BoardSize * _stage.BoardSize) - 1;
        int swordCount = _stage.MonsterCount * GameConst.SwordTileRatio;

        for (int i = 0; i < tileCount; i++)
        {
            var position = CoordToPosition(IndexToCoord(i));
            Vector3 scale = Vector3.one * tileSize;
            scale.z = 1f;

            Tile tile = null;

            if(i < currentMonsterCount)
            {
                tile = factory.Create(TileFactory.TileType.Monster, i, _stage.monsters[i]);
            }
            else if(i - _stage.MonsterCount < swordCount)
            {
                tile = factory.Create(TileFactory.TileType.Sword, i);
                //tile = factory.Create(TileFactory.TileType.Bomb, i, stage.BoardSize);
            }
            else
            {
                tile = factory.Create(TileFactory.TileType.Normal, i);
            }

            TileController tc = Instantiate(tilePrefab, transform);
            tc.gameObject.SetActive(false);
            tc.SetData(tile, position, scale);

            tiles.Add(tc);
        }
    }

    public IEnumerator Shuffle(string _animationName)
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            while(GameManager.Instance.IsPause)
            {
                yield return null;
            }

            int index1 = i;
            int index2 = Random.Range(0, tiles.Count);
            var coord1 = IndexToCoord(tiles[index1].Index);
            var coord2 = IndexToCoord(tiles[index2].Index);

            bool isEdge1 = (coord1.x == 0 || coord1.y == 0 || coord1.x == stage.BoardSize - 1 || coord1.y == stage.BoardSize - 1);

            bool isEdge2 = (coord2.x == 0 || coord2.y == 0 || coord2.x == stage.BoardSize - 1 || coord2.y == stage.BoardSize - 1);


            if ((tiles[index1].Type.Equals(typeof(SwordTile)) && !isEdge2)
                || (tiles[index2].Type.Equals(typeof(SwordTile)) && !isEdge1)
                || (tiles[index1].Type.Equals(typeof(MonsterTile)) && isEdge2)
                || (tiles[index2].Type.Equals(typeof(MonsterTile)) && isEdge1))
            {
                // 다시 선택
                i--;
                continue;
            }


            var beforeTile = tiles[index1];
            var afterTile = tiles[index2];

            var tempIndex = beforeTile.Index;
            beforeTile.data.index = afterTile.Index;
            afterTile.data.index = tempIndex;
            
            beforeTile.transform.localPosition = CoordToPosition(IndexToCoord(beforeTile.Index));

            afterTile.transform.localPosition = CoordToPosition(IndexToCoord(afterTile.Index));

            beforeTile.gameObject.SetActive(false);
        }

        tiles.Sort(delegate (TileController a, TileController b)
        {
            return a.Index.CompareTo(b.Index);
        });

        if (string.IsNullOrEmpty(_animationName) == false)
        {
            for (int i = 0; i < tiles.Count; i++)
            {
                while (GameManager.Instance.IsPause)
                {
                    yield return null;
                }

                tiles[i].gameObject.SetActive(true);
                tiles[i].PlayAnimation(_animationName);
                yield return new WaitForSeconds(TILE_CREATE_DELAY / stage.BoardSize);
            }
        }

        createComplete = true;
    }
    
    public IEnumerator MoveTile(int _selectIndex)
    {
        isMoved = true;

        var selectCoord = IndexToCoord(_selectIndex);
        var blankCoord = IndexToCoord(blankIndex);

        Vector3 dir = blankCoord - selectCoord;
        float dot = Vector3.Dot(dir.normalized, Vector3.right);
        int count = (int)dir.magnitude;

        if (Mathf.Abs(dot) == 1f || dot == 0f )
        {
            if(dot == 0)
            {
                yield return MoveVertical(dir, _selectIndex, count);
            }
            else
            {
                yield return MoveHorizontal(dir, _selectIndex, count);
            }

            blankIndex = _selectIndex;
        }

        #region single tile moved

        // var magnitude = (selectCoord - blankCoord).sqrMagnitude;

        //if (magnitude == 1)
        //{
        //    var selectTile = tiles.Find(x => x.Index == _selectIndex);
        //    Vector3 direction = blankCoord - selectCoord;
        //    direction.y *= -1;

        //    yield return selectTile.MoveCoroutine(direction);

        //    selectTile.data.index = blankIndex;

        //    blankIndex = _selectIndex;
        //}
        #endregion

        isMoved = false;
    }

    private IEnumerator MoveVertical(Vector3 _dir, int _selectIndex, int _count)
    {
        _dir.y *= -1;

        for (int i = _count - 1; i >= 0; i--)
        {
            TileController tile = null;
            int index = 0;
            int swapIndex = 0;


            if (_selectIndex > blankIndex)
            {
                index = _selectIndex - i * stage.BoardSize;
                swapIndex = _selectIndex - (i + 1) * stage.BoardSize;
            }
            else
            {
                index = _selectIndex + i * stage.BoardSize;
                swapIndex = _selectIndex + (i + 1) * stage.BoardSize;
            }

            tile = tiles.Find(x => x.Index == index);

            StartCoroutine(tile.MoveCoroutine(_dir.normalized));

            tile.data.index = swapIndex;
        }

        yield return null;
    }

    private IEnumerator MoveHorizontal(Vector3 _dir, int _selectIndex, int _count)
    {
        for (int i = _count - 1; i >= 0; i--)
        {
            TileController tile = null;
            int index = 0;
            int swapIndex = 0;

            if(_selectIndex > blankIndex)
            {
                index = _selectIndex - i;
                swapIndex = _selectIndex - (i + 1);
            }
            else
            {
                index = _selectIndex + i;
                swapIndex = _selectIndex + (i + 1);
            }

            tile = tiles.Find(x => x.Index == index);

            StartCoroutine(tile.MoveCoroutine(_dir.normalized));

            tile.data.index = swapIndex;
        }

        yield return null;
    }

    public void Clear()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            var tile = tiles[i];
            Destroy(tile.gameObject);
        }

        tiles.Clear();
        tiles = null;
        GameManager.Instance.currentPuzzle = null;
        factory = null;
    }

    private bool CheckMonsterCount()
    {
        var monsters = tiles.FindAll(x => x.data.GetType().Equals(typeof(MonsterTile)));
        
        for (int i = 0; i < monsters.Count; i++)
        {
            var monster = monsters[i];
            var data = (MonsterTile)monster.data;

            if (data.IsDead)
            {
                var transform = monster.transform;

                currentMonsterCount--;

                ChangeTileType(monster, TileFactory.TileType.Normal);
            }
        }

        return (currentMonsterCount <= 0);
    }

    public List<TileController> GetWeaponTiles()
    {
        var weapons = new List<TileController>();

        for (int i = 0; i < tiles.Count; i++)
        {
            if(tiles[i] == null)
            {
                continue;
            }

            if (tiles[i].data.GetType().IsSubclassOf(typeof(WeaponTile)))
            {
                weapons.Add(tiles[i]);
            }
        }

        return weapons;
    }
    
    public void StartAttack()
    {
        if(isAttack)
        {
            return;
        }

        isAttack = true;

        tiles.Sort(delegate (TileController a, TileController b)
        {
            return a.Index.CompareTo(b.Index);
        });

        StartCoroutine(Attack());
    }

    public IEnumerator Attack()
    {
        GameManager.Instance.ChangeState(GameManager.PlayState.Attack);

        while(isMoved)
        {
            yield return null;
        }

        var weapons = GetWeaponTiles();

        if(isAttackLimit)
        {
            currentAttackLimit--;
        }

        foreach (var weapon in weapons)
        {
            while(GameManager.Instance.IsPause)
            {
                yield return null;
            }

            var scopes = new List<Tile>();
            var attackRange = ((WeaponTile)weapon.data).AttackRanges;

            foreach (var range in attackRange)
            {
                Vector2 targetCoord = IndexToCoord(weapon.Index) + range;

                if (0 <= targetCoord.x && targetCoord.x < stage.BoardSize
                    && 0 <= targetCoord.y && targetCoord.y < stage.BoardSize)
                {
                    int targetIndex = CoordToIndex(targetCoord);
                    var tile = tiles.Find(x => x.Index == targetIndex);

                    if (tile == null)
                    {
                        scopes.Add(null);
                    }
                    else
                    {
                        scopes.Add(tile.data);
                    }
                }
            }

            yield return StartCoroutine(((WeaponTile)weapon.data).Attack(scopes));
        }

        if (isAttackLimit)
        {
            if (currentAttackLimit == 0)
            {
                GameManager.Instance.FinishGame(CheckMonsterCount());
                //StopCoroutine(attackCoroutine);
            }
            else if(CheckMonsterCount())
            {
                GameManager.Instance.FinishGame(true);
            }
            else
            {
                yield return Shuffle("Tile_Respawn");

                if (GameManager.Instance.IsPause == false)
                {
                    GameManager.Instance.ChangeState(GameManager.PlayState.Play);
                }
            }
        }
        else if (CheckMonsterCount())
        {
            GameManager.Instance.FinishGame(true);
            //StopCoroutine(attackCoroutine);
        }
        else
        {
            yield return Shuffle("Tile_Respawn");

            if (GameManager.Instance.IsPause == false)
            {
                GameManager.Instance.ChangeState(GameManager.PlayState.Play);
            }
        }

        GameManager.Reinforce = 1;

        isAttack = false;
    }

    public TileController GetRandomTile()
    {
        var normalTiles = tiles.FindAll(x => x.Type.Equals(typeof(NormalTile)));

        int randomIndex = Random.Range(0, normalTiles.Count);

        return normalTiles[randomIndex];
    }

    public void ChangeTileType(TileController _tile, TileFactory.TileType _changedType, params object[] _params)
    {
        var transform = _tile.transform;
        var newTileData = factory.Create(_changedType, _tile.Index, _params);

        int listIndex = tiles.FindIndex(x => x.Index == _tile.Index);

        Destroy(_tile.gameObject);
        tiles.RemoveAt(listIndex);

        var newTile = Instantiate(tilePrefab, this.transform);
        newTile.name = "new tile";
        newTile.SetData(newTileData, transform.localPosition, transform.localScale);
        newTile.PlayAnimation("Tile_Respawn");
        tiles.Insert(listIndex, newTile);
    }

    // =============================== 좌표관련 ===============================

    private Vector2 IndexToCoord(int _index)
    {
        int x = _index == 0 ? 0 : _index % stage.BoardSize;
        int y = _index == 0 ? 0 : _index / stage.BoardSize;

        return new Vector2(x, y);
    }

    private int CoordToIndex(Vector2 _coord)
    {
        return (int)(_coord.y * stage.BoardSize + _coord.x);
    }

    private Vector2 CoordToPosition(Vector2 _coord)
    {
        return new Vector2(startPos.x + (tileSize * 2f) * _coord.x, startPos.y - (tileSize * 2f) * _coord.y);
    }
}
