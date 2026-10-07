using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SteelFlameAbyss.Battle
{
    /// <summary>
    /// 한 번의 전투에서 사용하는 카드 더미와 손패 상태를 관리합니다.
    /// </summary>
    public sealed class DeckState
    {
        private readonly List<CardInstance> drawPile = new();
        private readonly List<CardInstance> hand = new();
        private readonly List<CardInstance> discardPile = new();
        private readonly List<CardInstance> exhaustPile = new();

        private readonly Random random;

        private readonly ReadOnlyCollection<CardInstance> drawPileView;
        private readonly ReadOnlyCollection<CardInstance> handView;
        private readonly ReadOnlyCollection<CardInstance> discardPileView;
        private readonly ReadOnlyCollection<CardInstance> exhaustPileView;

        public IReadOnlyList<CardInstance> DrawPile => drawPileView;
        public IReadOnlyList<CardInstance> Hand => handView;
        public IReadOnlyList<CardInstance> DiscardPile => discardPileView;
        public IReadOnlyList<CardInstance> ExhaustPile => exhaustPileView;

        public int TotalCardCount =>
            drawPile.Count + hand.Count + discardPile.Count + exhaustPile.Count;

        public DeckState(IEnumerable<CardInstance> startingDeck)
            : this(startingDeck, new Random())
        {
        }

        /// <summary>
        /// 테스트에서 동일한 셔플 결과가 필요하면 seed를 지정한 Random을 전달할 수 있습니다.
        /// </summary>
        public DeckState(IEnumerable<CardInstance> startingDeck, Random random)
        {
            if (startingDeck == null)
                throw new ArgumentNullException(nameof(startingDeck));

            this.random = random ?? throw new ArgumentNullException(nameof(random));

            foreach (CardInstance card in startingDeck)
            {
                if (card == null)
                    throw new ArgumentException("시작 덱에는 null 카드가 들어갈 수 없습니다.", nameof(startingDeck));

                drawPile.Add(card);
            }

            drawPileView = drawPile.AsReadOnly();
            handView = hand.AsReadOnly();
            discardPileView = discardPile.AsReadOnly();
            exhaustPileView = exhaustPile.AsReadOnly();

            ShuffleDrawPile();
        }

        /// <summary>
        /// 지정한 수만큼 카드를 뽑습니다. 카드가 부족하면 버린 더미를 섞어 뽑기 더미로 옮깁니다.
        /// 실제로 뽑은 카드 목록을 반환합니다.
        /// </summary>
        public IReadOnlyList<CardInstance> Draw(int count)
        {
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "뽑을 카드 수는 0 이상이어야 합니다.");

            var drawnCards = new List<CardInstance>(count);

            for (int i = 0; i < count && TryDraw(out CardInstance card); i++)
                drawnCards.Add(card);

            return drawnCards;
        }

        public bool TryDraw(out CardInstance card)
        {
            if (drawPile.Count == 0 && !ShuffleDiscardIntoDrawPile())
            {
                card = null;
                return false;
            }

            int topIndex = drawPile.Count - 1;
            card = drawPile[topIndex];
            drawPile.RemoveAt(topIndex);
            hand.Add(card);
            return true;
        }

        /// <summary>손패의 카드를 버린 더미로 옮깁니다.</summary>
        public bool Discard(CardInstance card)
        {
            return MoveFromHand(card, discardPile);
        }

        /// <summary>손패의 카드를 이번 전투의 소멸 더미로 옮깁니다.</summary>
        public bool Exhaust(CardInstance card)
        {
            return MoveFromHand(card, exhaustPile);
        }

        public void DiscardHand()
        {
            discardPile.AddRange(hand);
            hand.Clear();
        }

        public void ShuffleDrawPile()
        {
            for (int i = drawPile.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (drawPile[i], drawPile[swapIndex]) = (drawPile[swapIndex], drawPile[i]);
            }
        }

        private bool ShuffleDiscardIntoDrawPile()
        {
            if (discardPile.Count == 0)
                return false;

            drawPile.AddRange(discardPile);
            discardPile.Clear();
            ShuffleDrawPile();
            return true;
        }

        private bool MoveFromHand(CardInstance card, List<CardInstance> destination)
        {
            if (card == null || !hand.Remove(card))
                return false;

            destination.Add(card);
            return true;
        }
    }
}
