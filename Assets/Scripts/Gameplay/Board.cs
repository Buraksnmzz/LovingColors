using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using DG.Tweening;
using Gameplay.Levels;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Gameplay
{
    [RequireComponent(typeof(GridLayoutGroup))]
    public class Board : MonoBehaviour
    {
        private const float ShuffleHideDuration = 0.7f;
        private const float ShuffleShowDuration = 0.7f;
        private const float ShuffleStepDelay = 0.06f;
        private const float SwapDuration = 0.22f;

        [SerializeField] private Card cardPrefab;
        [SerializeField] private RectTransform boardRect;
        [SerializeField] private GridLayoutGroup gridLayoutGroup;
        [SerializeField] private Button reshuffleButton;

        private int _rowCount;
        private int _columnCount;
        private Color _colorA;
        private Color _colorB;
        private Color _colorC;
        private Color _colorD;
        private List<Card> _cards;
        private readonly List<Vector2> _slotPositions = new();
        private Vector2 _cellSize;
        private Card _selectedCard;
        private Card _draggedCard;
        private Card _currentDragTarget;
        private bool _isInteractionLocked;


        private void Awake()
        {
            _cards = new List<Card>();
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

        public void Initialize(LevelDefinition levelDefinition)
        {
            if (levelDefinition == null)
            {
                Debug.LogError("Board could not be initialized because level definition is null.");
                return;
            }

            _rowCount = levelDefinition.RowCount;
            _columnCount = levelDefinition.ColumnCount;
            _colorA = levelDefinition.TopLeftColor;
            _colorB = levelDefinition.TopRightColor;
            _colorC = levelDefinition.BottomLeftColor;
            _colorD = levelDefinition.BottomRightColor;

            ConfigureGrid();
            CreateCards();
            _cellSize = new Vector2(boardRect.rect.width / _columnCount, boardRect.rect.height / _rowCount);
            gridLayoutGroup.cellSize = _cellSize;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
            CacheSlotPositions();
            AssignColors();
            LockCards(levelDefinition.RuleName);
            SnapAllCardsToSlots();
            DOVirtual.DelayedCall(0.6f, StartShuffleAnimation);
        }

        private void CreateCards()
        {
            ClearCards();

            var totalCardCount = _rowCount * _columnCount;
            for (var index = 0; index < totalCardCount; index++)
            {
                var card = CreateCardInstance(index);
                card.CardId = index;
                card.Order = index;
                card.IsLocked = false;
                card.IsSelected = false;
                card.IsClickable = true;
                SubscribeToCard(card);
                _cards.Add(card);
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

        private void ConfigureGrid()
        {
            gridLayoutGroup.enabled = true;
            gridLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayoutGroup.constraintCount = _columnCount;
        }

        private void ClearCards()
        {
            StopAllCoroutines();

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

            var hideSequence = DOTween.Sequence();
            for (var index = 0; index < unlockedCards.Count; index++)
            {
                var card = unlockedCards[index];
                hideSequence.Insert(index * ShuffleStepDelay,
                    card.transform.DOScale(Vector3.zero, ShuffleHideDuration)
                        .SetEase(Ease.OutCubic));
            }

            yield return hideSequence.WaitForCompletion();

            var shuffledCards = new List<Card>(unlockedCards);
            ShuffleCards(shuffledCards);

            for (var index = 0; index < unlockedIndices.Count; index++)
            {
                var boardIndex = unlockedIndices[index];
                var card = shuffledCards[index];
                _cards[boardIndex] = card;
                card.Order = boardIndex;
                card.SnapTo(_slotPositions[boardIndex]);
            }
            NormalizeSiblingOrder();

            var showSequence = DOTween.Sequence();
            for (var index = 0; index < shuffledCards.Count; index++)
            {
                var card = shuffledCards[index];
                showSequence.Insert(index * ShuffleStepDelay,
                    card.transform.DOScale(Vector3.one, ShuffleShowDuration)
                        .SetEase(Ease.OutCubic));
            }

            yield return showSequence.WaitForCompletion();
            RestoreCardInteractivity();
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
            for (var index = 0; index < _cards.Count; index++)
            {
                _slotPositions.Add(_cards[index].RectTransform.anchoredPosition);
            }

            gridLayoutGroup.enabled = false;
        }

        private void SnapAllCardsToSlots()
        {
            for (var index = 0; index < _cards.Count; index++)
            {
                _cards[index].SnapTo(_slotPositions[index]);
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
        }

        private Card FindCurrentDragTarget(Card draggedCard)
        {
            Card nearestCard = null;
            var smallestDistance = float.MaxValue;
            var draggedPosition = draggedCard.RectTransform.anchoredPosition;
            var threshold = Mathf.Max(_cellSize.x, _cellSize.y) * 0.55f;

            foreach (var card in _cards)
            {
                if (card == draggedCard || card.IsLocked)
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
            sequence.OnComplete(() =>
            {
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

        private Card CreateCardInstance(int index)
        {
            return Instantiate(cardPrefab, transform);
        }
    }
}