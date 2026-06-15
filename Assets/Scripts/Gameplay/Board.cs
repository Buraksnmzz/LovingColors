using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using DG.Tweening;
using Gameplay.Layouts;
using Gameplay.Levels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Gameplay
{
    [RequireComponent(typeof(GridLayoutGroup))]
    public class Board : MonoBehaviour
    {
        private const float WinAnimationStartDelay = 0.5f;
        private const float WinColumnRotateDuration = 0.6f;
        private const float WinAnimationColumnDelay = 0.2f;
        private const float WinAnimationCardDelay = 0.03f;
        private const float ShuffleHideDuration = 0.7f;
        private const float ShuffleShowDuration = 0.7f;
        private const float ShuffleWaveDuration = 1.2f;
        private const float SwapDuration = 0.22f;

        [SerializeField] private Card cardPrefab;
        [SerializeField] private ShapeCardCatalog shapeCardCatalog;
        [SerializeField] private RectTransform boardRect;
        [SerializeField] private GridLayoutGroup gridLayoutGroup;
        [SerializeField] private Button reshuffleButton;
        [SerializeField] private ParticleSystem winParticle;
        [SerializeField] private bool showDebugSlotIndices;

        private int _rowCount;
        private int _columnCount;
        private Color _colorA;
        private Color _colorB;
        private Color _colorC;
        private Color _colorD;
        private List<Card> _cards;
        private readonly List<Vector2> _slotPositions = new();
        private readonly List<float> _slotRotations = new();
        private readonly List<int> _slotPieceTypes = new();
        private readonly List<Vector2> _slotSizes = new();
        private string _currentShapeId;
        private Vector2 _cellSize;
        private Card _selectedCard;
        private Card _draggedCard;
        private Card _currentDragTarget;
        private Tween _shuffleDelayTween;
        private bool _isInteractionLocked;
        private bool _hasCompletedBoard;
        private bool _usesCustomLayout;
        private Card _activeCardPrefab;

        public event Action Solved;
        public event Action WinSequenceCompleted;


        private void Awake()
        {
            _cards = new List<Card>();
            _activeCardPrefab = cardPrefab;
            reshuffleButton.onClick.AddListener(Reshuffle);
        }

        private void OnDestroy()
        {
            reshuffleButton.onClick.RemoveListener(Reshuffle);
        }

        private void Reshuffle()
        {
            StartShuffleAnimation();
        }

        public void UseHint()
        {
            if (_isInteractionLocked || _draggedCard != null || _hasCompletedBoard)
            {
                return;
            }

            var targetIndex = GetFirstMisplacedCardIndex();
            if (targetIndex < 0)
            {
                return;
            }

            var currentCard = _cards[targetIndex];
            var correctCard = FindCardById(targetIndex);
            if (correctCard == null || correctCard == currentCard || currentCard.IsLocked || correctCard.IsLocked)
            {
                return;
            }

            StartSwap(currentCard, correctCard);
        }

        public void CompleteImmediately()
        {
            if (_hasCompletedBoard || _cards == null || _cards.Count == 0)
            {
                return;
            }

            StopAllCoroutines();
            ClearSelection();
            ClearCurrentDragTarget();
            _draggedCard = null;

            var solvedCards = new Card[_cards.Count];
            for (var index = 0; index < _cards.Count; index++)
            {
                var card = _cards[index];
                card.transform.DOKill();
                card.transform.localScale = Vector3.one;
                card.transform.localRotation = Quaternion.identity;
                solvedCards[card.CardId] = card;
            }

            for (var index = 0; index < solvedCards.Length; index++)
            {
                var card = solvedCards[index];
                _cards[index] = card;
                card.Order = index;
                card.SnapTo(_slotPositions[index]);
            }

            NormalizeSiblingOrder();
            TryCompleteBoard();
        }

        public void Initialize(LevelDefinition levelDefinition)
        {
            _hasCompletedBoard = false;
            _usesCustomLayout = false;
            _rowCount = levelDefinition.RowCount;
            _columnCount = levelDefinition.ColumnCount;
            _colorA = levelDefinition.TopLeftColor;
            _colorB = levelDefinition.TopRightColor;
            _colorC = levelDefinition.BottomLeftColor;
            _colorD = levelDefinition.BottomRightColor;

            if (reshuffleButton != null)
            {
                reshuffleButton.interactable = true;
            }

            var customLayout = GetCustomLayout(levelDefinition);
            if (customLayout != null)
            {
                _usesCustomLayout = true;
                _currentShapeId = levelDefinition.ShapeId;
                CacheCustomPieceTypes(customLayout.SlotPieceTypes);
                CacheCustomSlotSizes(customLayout.SlotSizes);
                CreateCards(customLayout.SlotCount);
                AssignColorsByPosition(customLayout.SlotPositions);
                LockCustomSlots(levelDefinition.LockedSlots);
                ShowDebugSlotIndices();
                StartCoroutine(PrepareCustomLayoutAndShuffleRoutine(customLayout));
                return;
            }

            _activeCardPrefab = cardPrefab;
            _currentShapeId = null;
            _slotPieceTypes.Clear();
            _slotSizes.Clear();

            ConfigureGrid();
            CreateCards(_rowCount * _columnCount);
            AssignColors();
            LockCards(levelDefinition.RuleName);
            StartCoroutine(PrepareLayoutAndShuffleRoutine());
        }

        private IEnumerator PrepareLayoutAndShuffleRoutine()
        {
            yield return null;

            Canvas.ForceUpdateCanvases();
            _cellSize = new Vector2(boardRect.rect.width / _columnCount, boardRect.rect.height / _rowCount);
            gridLayoutGroup.cellSize = _cellSize;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
            Canvas.ForceUpdateCanvases();
            CacheSlotPositions();
            SnapAllCardsToSlots();
            _shuffleDelayTween = DOVirtual.DelayedCall(0.6f, StartShuffleAnimation);
        }

        private IEnumerator PrepareCustomLayoutAndShuffleRoutine(BoardLayoutResult layoutResult)
        {
            yield return null;

            Canvas.ForceUpdateCanvases();
            _cellSize = layoutResult.CellSize;
            gridLayoutGroup.enabled = false;
            ApplyCustomCardSizing();
            CacheCustomSlotPositions(layoutResult.SlotPositions);
            CacheCustomSlotRotations(layoutResult.SlotRotations);
            SnapAllCardsToSlots();
            _shuffleDelayTween = DOVirtual.DelayedCall(0.6f, StartShuffleAnimation);
        }

        private void CreateCards(int totalCardCount)
        {
            ClearCards();

            for (var index = 0; index < totalCardCount; index++)
            {
                var card = CreateCardInstance(index);
                card.CardId = index;
                card.Order = index;
                card.PieceType = GetSlotPieceType(index);
                card.IsLocked = false;
                card.IsSelected = false;
                card.IsClickable = true;
                SubscribeToCard(card);
                _cards.Add(card);
            }
        }

        private void LockCustomSlots(int[] lockedSlots)
        {
            foreach (var card in _cards)
            {
                card.IsLocked = false;
            }

            if (lockedSlots == null)
            {
                return;
            }

            for (var index = 0; index < lockedSlots.Length; index++)
            {
                var slotIndex = lockedSlots[index];
                if (slotIndex < 0 || slotIndex >= _cards.Count)
                {
                    Debug.LogWarning($"Locked slot index '{slotIndex}' is out of range for the current board.");
                    continue;
                }

                _cards[slotIndex].IsLocked = true;
            }
        }

        private void LockCards(string ruleName)
        {
            foreach (var card in _cards)
            {
                card.IsLocked = false;
            }

            if (!LockRuleRegistry.TryGet(ruleName, out var lockRule))
            {
                Debug.LogWarning($"Lock rule '{ruleName}' was not found. Falling back to corner lock rule.");
                new LockCorners().LockCards(_cards, _columnCount, _rowCount);
                return;
            }

            lockRule.LockCards(_cards, _columnCount, _rowCount);
        }

        private void AssignColors()
        {
            for (int r = 0; r < _rowCount; r++)
            {
                for (int c = 0; c < _columnCount; c++)
                {
                    float aContribution = Mathf.Clamp01(1f - (float)c / (_columnCount - 1)) * Mathf.Clamp01(1f - (float)r / (_rowCount - 1));
                    float bContribution = Mathf.Clamp01((float)c / (_columnCount - 1)) * Mathf.Clamp01(1f - (float)r / (_rowCount - 1));
                    float cContribution = Mathf.Clamp01(1f - (float)c / (_columnCount - 1)) * Mathf.Clamp01((float)r / (_rowCount - 1));
                    float dContribution = Mathf.Clamp01((float)c / (_columnCount - 1)) * Mathf.Clamp01((float)r / (_rowCount - 1));
                    Color finalColor = _colorA * aContribution + _colorB * bContribution + _colorC * cContribution + _colorD * dContribution;
                    _cards[r * _columnCount + c].GetComponent<Image>().color = finalColor;
                }
            }
        }

        private void AssignColorsByPosition(List<Vector2> slotPositions)
        {
            if (slotPositions == null || slotPositions.Count == 0)
            {
                return;
            }

            var minX = float.MaxValue;
            var maxX = float.MinValue;
            var minY = float.MaxValue;
            var maxY = float.MinValue;

            for (var index = 0; index < slotPositions.Count; index++)
            {
                var position = slotPositions[index];
                minX = Mathf.Min(minX, position.x);
                maxX = Mathf.Max(maxX, position.x);
                minY = Mathf.Min(minY, position.y);
                maxY = Mathf.Max(maxY, position.y);
            }

            var width = Mathf.Max(0.0001f, maxX - minX);
            var height = Mathf.Max(0.0001f, maxY - minY);

            for (var index = 0; index < _cards.Count; index++)
            {
                var position = slotPositions[index];
                var u = Mathf.Clamp01((position.x - minX) / width);
                var v = Mathf.Clamp01((maxY - position.y) / height);
                var topColor = Color.Lerp(_colorA, _colorB, u);
                var bottomColor = Color.Lerp(_colorC, _colorD, u);
                var finalColor = Color.Lerp(topColor, bottomColor, v);
                _cards[index].GetComponent<Image>().color = finalColor;
            }
        }

        private void ConfigureGrid()
        {
            gridLayoutGroup.enabled = true;
            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = _columnCount;
        }

        private void ClearCards()
        {
            StopAllCoroutines();
            _shuffleDelayTween?.Kill();
            _shuffleDelayTween = null;

            if (_cards == null)
            {
                _cards = new List<Card>();
            }

            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                var child = transform.GetChild(index);
                child.DOKill();
                Destroy(child.gameObject);
            }

            foreach (var card in _cards)
            {
                UnsubscribeFromCard(card);
            }

            _selectedCard = null;
            _draggedCard = null;
            _currentDragTarget = null;
            _isInteractionLocked = false;
            _hasCompletedBoard = false;
            _cards.Clear();
            _slotPositions.Clear();
        }

        private void StartShuffleAnimation()
        {
            StopAllCoroutines();
            StartCoroutine(ShuffleUnlockedCardsRoutine());
        }

        private IEnumerator ShuffleUnlockedCardsRoutine()
        {
            var unlockedCards = new List<Card>();
            var unlockedIndices = new List<int>();

            for (var index = 0; index < _cards.Count; index++)
            {
                var card = _cards[index];
                card.transform.DOKill();
                card.transform.localScale = Vector3.one;
                card.IsClickable = false;

                if (card.IsLocked)
                {
                    continue;
                }

                unlockedCards.Add(card);
                unlockedIndices.Add(index);
            }

            if (unlockedCards.Count <= 1)
            {
                RestoreCardInteractivity();
                yield break;
            }

            var shuffleAnimationOrder = GetShuffleAnimationOrder(unlockedIndices);
            var shuffleStepDelay = GetShuffleStepDelay(shuffleAnimationOrder.Count);

            var hideSequence = DOTween.Sequence();
            for (var index = 0; index < shuffleAnimationOrder.Count; index++)
            {
                var card = _cards[shuffleAnimationOrder[index]];
                hideSequence.Insert(index * shuffleStepDelay,
                    card.transform.DOScale(Vector3.zero, ShuffleHideDuration)
                        .SetEase(Ease.OutCubic));
            }

            yield return hideSequence.WaitForCompletion();

            var shuffledCards = new List<Card>(unlockedCards);
            ShuffleCardsWithinPieceTypes(shuffledCards, unlockedIndices);

            for (var index = 0; index < unlockedIndices.Count; index++)
            {
                var boardIndex = unlockedIndices[index];
                var card = shuffledCards[index];
                _cards[boardIndex] = card;
                card.Order = boardIndex;
                card.SnapTo(_slotPositions[boardIndex]);
                card.SnapRotation(GetSlotRotation(boardIndex));
            }
            NormalizeSiblingOrder();

            var showSequence = DOTween.Sequence();
            for (var index = 0; index < shuffleAnimationOrder.Count; index++)
            {
                var card = _cards[shuffleAnimationOrder[index]];
                showSequence.Insert(index * shuffleStepDelay,
                    card.transform.DOScale(Vector3.one, ShuffleShowDuration)
                        .SetEase(Ease.OutCubic));
            }

            yield return showSequence.WaitForCompletion();
            RestoreCardInteractivity();
        }

        private List<int> GetShuffleAnimationOrder(List<int> boardIndices)
        {
            if (_usesCustomLayout)
            {
                return GetPositionBasedAnimationOrder(boardIndices);
            }

            var orderedIndices = new List<int>(boardIndices);
            orderedIndices.Sort((firstIndex, secondIndex) =>
            {
                var firstRow = firstIndex / _columnCount;
                var firstColumn = firstIndex % _columnCount;
                var secondRow = secondIndex / _columnCount;
                var secondColumn = secondIndex % _columnCount;
                var firstDiagonal = firstRow + firstColumn;
                var secondDiagonal = secondRow + secondColumn;

                if (firstDiagonal != secondDiagonal)
                {
                    return firstDiagonal.CompareTo(secondDiagonal);
                }

                return firstColumn.CompareTo(secondColumn);
            });

            return orderedIndices;
        }

        private List<int> GetPositionBasedAnimationOrder(List<int> boardIndices)
        {
            var orderedIndices = new List<int>(boardIndices);
            orderedIndices.Sort((firstIndex, secondIndex) =>
            {
                var firstPosition = _slotPositions[firstIndex];
                var secondPosition = _slotPositions[secondIndex];
                var firstDiagonal = -firstPosition.y + firstPosition.x;
                var secondDiagonal = -secondPosition.y + secondPosition.x;
                var diagonalComparison = firstDiagonal.CompareTo(secondDiagonal);
                if (diagonalComparison != 0)
                {
                    return diagonalComparison;
                }

                return firstPosition.x.CompareTo(secondPosition.x);
            });

            return orderedIndices;
        }

        private static float GetShuffleStepDelay(int cardCount)
        {
            if (cardCount <= 1)
            {
                return 0f;
            }

            return ShuffleWaveDuration / (cardCount - 1);
        }

        private void RestoreCardInteractivity()
        {
            foreach (var card in _cards)
            {
                card.IsClickable = !_isInteractionLocked && _draggedCard == null && !card.IsLocked;
            }
        }

        private void CacheSlotPositions()
        {
            _slotPositions.Clear();
            _slotRotations.Clear();
            for (var index = 0; index < _cards.Count; index++)
            {
                _slotPositions.Add(_cards[index].RectTransform.anchoredPosition);
            }

            gridLayoutGroup.enabled = false;
        }

        private void CacheCustomSlotPositions(List<Vector2> slotPositions)
        {
            _slotPositions.Clear();
            var boardOffset = boardRect != null ? boardRect.anchoredPosition : Vector2.zero;
            for (var index = 0; index < slotPositions.Count; index++)
            {
                _slotPositions.Add(slotPositions[index] + boardOffset);
            }
        }

        private void CacheCustomSlotRotations(List<float> slotRotations)
        {
            _slotRotations.Clear();
            if (slotRotations == null)
            {
                return;
            }

            for (var index = 0; index < slotRotations.Count; index++)
            {
                _slotRotations.Add(slotRotations[index]);
            }
        }

        private float GetSlotRotation(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotRotations.Count)
            {
                return 0f;
            }

            return _slotRotations[slotIndex];
        }

        private void ApplyCustomCardSizing()
        {
            for (var index = 0; index < _cards.Count; index++)
            {
                var rectTransform = _cards[index].RectTransform;
                rectTransform.sizeDelta = GetSlotSize(index);
                _cards[index].SetBaseScale(Vector3.one);
            }
        }

        private Vector2 GetSlotSize(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotSizes.Count)
            {
                return _cellSize;
            }

            return _slotSizes[slotIndex];
        }

        private void SnapAllCardsToSlots()
        {
            for (var index = 0; index < _cards.Count; index++)
            {
                _cards[index].SnapTo(_slotPositions[index]);
                _cards[index].SnapRotation(GetSlotRotation(index));
                _cards[index].Order = index;
            }

            NormalizeSiblingOrder();
        }

        private void SubscribeToCard(Card card)
        {
            card.CardClicked += HandleCardClicked;
            card.DragStarted += HandleCardDragStarted;
            card.Dragged += HandleCardDragged;
            card.DragEnded += HandleCardDragEnded;
        }

        private void UnsubscribeFromCard(Card card)
        {
            card.CardClicked -= HandleCardClicked;
            card.DragStarted -= HandleCardDragStarted;
            card.Dragged -= HandleCardDragged;
            card.DragEnded -= HandleCardDragEnded;
        }

        private void HandleCardClicked(Card clickedCard)
        {
            if (_isInteractionLocked || _draggedCard != null || clickedCard.IsLocked)
            {
                return;
            }

            if (_selectedCard == null)
            {
                SelectCard(clickedCard);
                return;
            }

            if (_selectedCard == clickedCard)
            {
                ClearSelection();
                return;
            }

            if (_selectedCard.PieceType != clickedCard.PieceType)
            {
                // Different shapes cannot swap; move the selection to the new card.
                SelectCard(clickedCard);
                return;
            }

            StartSwap(_selectedCard, clickedCard);
        }

        private void HandleCardDragStarted(Card draggedCard)
        {
            if (_isInteractionLocked || draggedCard.IsLocked)
            {
                return;
            }

            ClearSelection();
            ClearCurrentDragTarget();
            _draggedCard = draggedCard;
            BringCardsToFront(draggedCard);

            foreach (var card in _cards)
            {
                if (card != draggedCard && !card.IsLocked)
                {
                    card.IsClickable = false;
                }
            }
        }

        private void HandleCardDragged(Card draggedCard)
        {
            if (_draggedCard != draggedCard)
            {
                return;
            }

            var nextTarget = FindCurrentDragTarget(draggedCard);
            if (_currentDragTarget == nextTarget)
            {
                return;
            }

            if (_currentDragTarget != null)
            {
                _currentDragTarget.SetTargetPreview(false);
            }

            _currentDragTarget = nextTarget;
            if (_currentDragTarget != null)
            {
                BringCardsToFront(draggedCard, _currentDragTarget);
                _currentDragTarget.SetTargetPreview(true);
            }
        }

        private void HandleCardDragEnded(Card draggedCard)
        {
            if (_draggedCard != draggedCard)
            {
                return;
            }

            draggedCard.StopDragSquashStretch();

            if (_currentDragTarget != null)
            {
                var swapTarget = _currentDragTarget;
                ClearCurrentDragTarget();
                StartSwap(draggedCard, swapTarget);
                _draggedCard = null;
                return;
            }

            draggedCard.TweenMoveTo(_slotPositions[draggedCard.Order], SwapDuration)
                .OnComplete(() =>
                {
                    _draggedCard = null;
                    RestoreCardInteractivity();
                    NormalizeSiblingOrder();
                });
            draggedCard.TweenRotateTo(GetSlotRotation(draggedCard.Order), SwapDuration);
        }

        private Card FindCurrentDragTarget(Card draggedCard)
        {
            Card nearestCard = null;
            var smallestDistance = float.MaxValue;
            var draggedPosition = draggedCard.RectTransform.anchoredPosition;
            var threshold = Mathf.Max(_cellSize.x, _cellSize.y) * 0.55f;

            foreach (var card in _cards)
            {
                if (card == draggedCard || card.IsLocked || card.PieceType != draggedCard.PieceType)
                {
                    continue;
                }

                var slotPosition = _slotPositions[card.Order];
                var distance = Vector2.Distance(draggedPosition, slotPosition);
                if (distance > threshold || distance >= smallestDistance)
                {
                    continue;
                }

                smallestDistance = distance;
                nearestCard = card;
            }

            return nearestCard;
        }

        private void StartSwap(Card firstCard, Card secondCard)
        {
            if (firstCard == null || secondCard == null || firstCard == secondCard)
            {
                return;
            }

            _isInteractionLocked = true;
            ClearSelection();
            ClearCurrentDragTarget();
            BringCardsToFront(firstCard, secondCard);

            var firstIndex = firstCard.Order;
            var secondIndex = secondCard.Order;
            var firstTargetPosition = _slotPositions[secondIndex];
            var secondTargetPosition = _slotPositions[firstIndex];

            _cards[firstIndex] = secondCard;
            _cards[secondIndex] = firstCard;
            firstCard.Order = secondIndex;
            secondCard.Order = firstIndex;

            var sequence = DOTween.Sequence();
            sequence.Join(firstCard.TweenMoveTo(firstTargetPosition, SwapDuration));
            sequence.Join(secondCard.TweenMoveTo(secondTargetPosition, SwapDuration));
            sequence.Join(firstCard.TweenRotateTo(GetSlotRotation(secondIndex), SwapDuration));
            sequence.Join(secondCard.TweenRotateTo(GetSlotRotation(firstIndex), SwapDuration));
            sequence.OnComplete(() =>
            {
                if (TryCompleteBoard())
                {
                    return;
                }

                _isInteractionLocked = false;
                _draggedCard = null;
                RestoreCardInteractivity();
                NormalizeSiblingOrder();
            });
        }

        private void SelectCard(Card card)
        {
            ClearSelection();
            _selectedCard = card;
            _selectedCard.IsSelected = true;
            BringCardsToFront(card);
        }

        private void ClearSelection()
        {
            if (_selectedCard == null)
            {
                return;
            }

            _selectedCard.IsSelected = false;
            _selectedCard = null;
            NormalizeSiblingOrder();
        }

        private void ClearCurrentDragTarget()
        {
            if (_currentDragTarget == null)
            {
                return;
            }

            _currentDragTarget.SetTargetPreview(false);
            _currentDragTarget = null;
        }

        private void BringCardsToFront(Card primaryCard, Card secondaryCard = null)
        {
            if (secondaryCard != null)
            {
                secondaryCard.transform.SetAsLastSibling();
            }

            primaryCard.transform.SetAsLastSibling();
        }

        private void NormalizeSiblingOrder()
        {
            if (_draggedCard != null || _selectedCard != null || _currentDragTarget != null)
            {
                return;
            }

            for (var index = 0; index < _cards.Count; index++)
            {
                _cards[index].transform.SetSiblingIndex(index);
            }
        }

        private static void ShuffleCards(List<Card> cards)
        {
            for (var index = cards.Count - 1; index > 0; index--)
            {
                var randomIndex = Random.Range(0, index + 1);
                (cards[index], cards[randomIndex]) = (cards[randomIndex], cards[index]);
            }

            var hasSameOrder = true;
            for (var index = 0; index < cards.Count; index++)
            {
                if (cards[index].Order == index)
                {
                    continue;
                }

                hasSameOrder = false;
                break;
            }

            if (hasSameOrder)
            {
                (cards[0], cards[^1]) = (cards[^1], cards[0]);
            }
        }

        private void ShuffleCardsWithinPieceTypes(List<Card> cards, List<int> slotIndices)
        {
            // Cards may only move to slots that share their piece type (e.g. a square
            // can never land on a deltoid slot). Shuffle each piece-type group on its
            // own set of positions. With a single piece type this is a full shuffle.
            var groups = new Dictionary<int, List<int>>();
            for (var positionIndex = 0; positionIndex < slotIndices.Count; positionIndex++)
            {
                var pieceType = GetSlotPieceType(slotIndices[positionIndex]);
                if (!groups.TryGetValue(pieceType, out var positions))
                {
                    positions = new List<int>();
                    groups[pieceType] = positions;
                }

                positions.Add(positionIndex);
            }

            foreach (var positions in groups.Values)
            {
                var groupCards = new List<Card>(positions.Count);
                for (var i = 0; i < positions.Count; i++)
                {
                    groupCards.Add(cards[positions[i]]);
                }

                ShuffleCards(groupCards);

                for (var i = 0; i < positions.Count; i++)
                {
                    cards[positions[i]] = groupCards[i];
                }
            }
        }

        private Card CreateCardInstance(int index)
        {
            if (_usesCustomLayout && shapeCardCatalog != null)
            {
                var prefab = shapeCardCatalog.ResolvePrefab(_currentShapeId, GetSlotPieceType(index));
                if (prefab != null)
                {
                    return Instantiate(prefab, transform);
                }

                Debug.LogWarning($"[Board] No prefab found in catalog for shape '{_currentShapeId}' (pieceType {GetSlotPieceType(index)}). Falling back to default prefab.");
            }

            return Instantiate(_activeCardPrefab, transform);
        }

        private int GetSlotPieceType(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _slotPieceTypes.Count)
            {
                return 0;
            }

            return _slotPieceTypes[slotIndex];
        }

        private void CacheCustomPieceTypes(List<int> slotPieceTypes)
        {
            _slotPieceTypes.Clear();
            if (slotPieceTypes == null)
            {
                return;
            }

            for (var index = 0; index < slotPieceTypes.Count; index++)
            {
                _slotPieceTypes.Add(slotPieceTypes[index]);
            }
        }

        private void CacheCustomSlotSizes(List<Vector2> slotSizes)
        {
            _slotSizes.Clear();
            if (slotSizes == null)
            {
                return;
            }

            for (var index = 0; index < slotSizes.Count; index++)
            {
                _slotSizes.Add(slotSizes[index]);
            }
        }

        private void ShowDebugSlotIndices()
        {
            if (!showDebugSlotIndices)
            {
                return;
            }

            for (var index = 0; index < _cards.Count; index++)
            {
                var labelObject = new GameObject($"DebugIndex_{index}", typeof(RectTransform));
                var labelRect = (RectTransform)labelObject.transform;
                labelRect.SetParent(_cards[index].transform, false);
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                labelRect.SetAsLastSibling();

                var label = labelObject.AddComponent<TextMeshProUGUI>();
                label.text = index.ToString();
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.black;
                label.fontSize = 40f;
                label.fontStyle = FontStyles.Bold;
                label.raycastTarget = false;
            }
        }

        private int GetFirstMisplacedCardIndex()
        {
            for (var index = 0; index < _cards.Count; index++)
            {
                var card = _cards[index];
                if (!card.IsLocked && card.CardId != index)
                {
                    return index;
                }
            }

            return -1;
        }

        private Card FindCardById(int cardId)
        {
            for (var index = 0; index < _cards.Count; index++)
            {
                if (_cards[index].CardId == cardId)
                {
                    return _cards[index];
                }
            }

            return null;
        }

        private bool TryCompleteBoard()
        {
            if (_hasCompletedBoard || !IsBoardSolved())
            {
                return false;
            }

            _hasCompletedBoard = true;
            LockInteractionForWin();
            Solved?.Invoke();
            PlayWinSequence();
            return true;
        }

        private bool IsBoardSolved()
        {
            for (var index = 0; index < _cards.Count; index++)
            {
                if (_cards[index].CardId != index)
                {
                    return false;
                }
            }

            return true;
        }

        private void LockInteractionForWin()
        {
            StopAllCoroutines();
            _isInteractionLocked = true;
            _draggedCard = null;
            ClearSelection();
            ClearCurrentDragTarget();

            if (reshuffleButton != null)
            {
                reshuffleButton.interactable = false;
            }

            foreach (var card in _cards)
            {
                card.transform.DOKill();
                card.IsLocked = false;
                card.IsClickable = false;
                card.StopDragSquashStretch();
                card.SetTargetPreview(false);
            }

            NormalizeSiblingOrder();
        }

        private void PlayWinSequence()
        {
            if (_usesCustomLayout)
            {
                PlayCustomWinSequence();
                return;
            }

            var sequence = DOTween.Sequence();
            sequence.AppendInterval(WinAnimationStartDelay);
            sequence.AppendCallback(() =>
            {
                if (winParticle != null)
                {
                    winParticle.Play();
                }
            });

            for (var column = 0; column < _columnCount; column++)
            {
                var columnDelay = column * WinAnimationColumnDelay;
                for (var row = 0; row < _rowCount; row++)
                {
                    var card = _cards[row * _columnCount + column];
                    var delay = columnDelay + row * WinAnimationCardDelay;
                    card.transform.localRotation = Quaternion.identity;
                    sequence.Insert(WinAnimationStartDelay + delay,
                        card.transform.DOLocalRotate(new Vector3(0f, 180f, 0f), WinColumnRotateDuration, RotateMode.FastBeyond360)
                            .SetEase(Ease.InOutQuad));
                }
            }

            sequence.OnComplete(() => WinSequenceCompleted?.Invoke());
        }

        private void PlayCustomWinSequence()
        {
            var sequence = DOTween.Sequence();
            sequence.AppendInterval(WinAnimationStartDelay);
            sequence.AppendCallback(() =>
            {
                if (winParticle != null)
                {
                    winParticle.Play();
                }
            });

            var orderedIndices = new List<int>(_cards.Count);
            for (var index = 0; index < _cards.Count; index++)
            {
                orderedIndices.Add(index);
            }

            orderedIndices.Sort((firstIndex, secondIndex) =>
            {
                var firstPosition = _slotPositions[firstIndex];
                var secondPosition = _slotPositions[secondIndex];
                var xComparison = firstPosition.x.CompareTo(secondPosition.x);
                if (xComparison != 0)
                {
                    return xComparison;
                }

                return secondPosition.y.CompareTo(firstPosition.y);
            });

            for (var index = 0; index < orderedIndices.Count; index++)
            {
                var card = _cards[orderedIndices[index]];
                var delay = index * WinAnimationCardDelay;
                card.transform.localRotation = Quaternion.identity;
                sequence.Insert(WinAnimationStartDelay + delay,
                    card.transform.DOLocalRotate(new Vector3(0f, 180f, 0f), WinColumnRotateDuration, RotateMode.FastBeyond360)
                        .SetEase(Ease.InOutQuad));
            }

            sequence.OnComplete(() => WinSequenceCompleted?.Invoke());
        }

        private BoardLayoutResult GetCustomLayout(LevelDefinition levelDefinition)
        {
            if (levelDefinition == null || !levelDefinition.HasCustomShape)
            {
                return null;
            }

            if (!PuzzleBoardLayoutRegistry.TryGet(levelDefinition.ShapeId, out var layout))
            {
                Debug.LogWarning($"Board layout '{levelDefinition.ShapeId}' was not found. Falling back to grid layout.");
                return null;
            }

            return layout.BuildLayout(boardRect.rect, _rowCount, _columnCount);
        }
    }
}