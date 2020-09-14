/*
Copyright (c) 2020 - 0xCD7F82BcFa333B4072A11Bd0B1da95c9b5f9E869 m/44'/60'/0'/0/0

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */

namespace RockStats.WebComponents {
    export const Dead = "0x000000000000000000000000000000000000dead";

    @Vidyano.WebComponents.WebComponent.register({
        properties: {
            transactions: {
                type: Object,
                readOnly: true
            },
            items: {
                type: Array,
                readOnly: true
            },
            isBusy: {
                type: Boolean,
                readOnly: true,
                value: true
            }
        }
    }, "rockstats")
    export class Transactions extends Vidyano.WebComponents.WebComponent {
        readonly transactions: Vidyano.Query; private _setTransactions: (transactions: Vidyano.Query) => void;
        readonly items: Vidyano.QueryResultItem[]; private _setItems: (transactions: Vidyano.QueryResultItem[]) => void;
        readonly isBusy: boolean; private _setIsBusy: (isBusy: boolean) => void;

        async attached() {
            super.attached();

            await this.app.initialize;
            this._setTransactions(await this.app.service.getQuery("e39930f9-b109-44e9-bfc9-a1a59d94ed5c"));

            const items = this.transactions.items;
            if (items.length < this.transactions.totalItems)
                this._applyLoadMore(items);

            this._setItems(items);

            const list = <any>Polymer.dom(this.root).querySelector("#list");
            list.scrollTarget = this.findParent(e => e instanceof Vidyano.WebComponents.Scroller, list)["scroller"];
            list.fire("iron-resize");

            this._setIsBusy(false);
        }

        private _sizeChanged() {
            (<any>Polymer.dom(this.root).querySelector("#list")).fire("iron-resize");
        }

        private _applyLoadMore(items: Vidyano.QueryResultItem[]) {
            items[items.length - 1] = new Proxy(items[items.length - 1], {
                get: (target, prop, receiver) => {
                    if (prop === "loadMore") {
                        this.set(`items.${items.length - 1}`, target);
                        const txs = this.transactions.clone();
                        txs.skip = items.length;
                        txs.search().then(newItems => {
                            const currentLength = items.length;
                            [].splice.apply(items, [items.length, 0].concat(<any>newItems));
                            if ((items.length + newItems.length) < this.transactions.totalItems)
                                this._applyLoadMore(items);

                            (<any>this).notifySplices("items", [
                                { index: currentLength, removed: [], addedCount: newItems.length, object: items, type: 'splice' }
                            ]);
                        });

                        return;
                    }

                    return target[prop];
                }
            });
        }

        private _transactionHref(transaction: Vidyano.QueryResultItem) {
            return `https://blockexplorer.mainnet.v1.omg.network/transaction/${transaction.values.Hash}`;
        }

        private _type(tx: Vidyano.QueryResultItem) {
            if (tx.values.Receiver === Dead && metadata_map[tx.values.Metadata] != null)
                return "Flair";
            
            return "Out";
        }

        private _avatar(transaction: Vidyano.QueryResultItem, property: string) {
            if (this._type(transaction) === "Flair" && property === "ReceiverAvatar")
                return flairs_map[metadata_map[transaction.values.Metadata]];

            return transaction.values[property]?.split("?")[0] || "";
        }

        private _amount(transaction: Vidyano.QueryResultItem) {
            var amount = transaction.values.AmountBigNumber;
            if (amount == null) {
                const div = new BigNumber("1000000000000000000");
                transaction.values.AmountBigNumber = (new BigNumber(transaction.values.Amount)).div(div);
            }

            const n: BigNumber = transaction.values.AmountBigNumber;
            if (n.lessThan(new BigNumber("0.0000001")))
                return "< 0.0000001";

            if (n.lessThan(10000))
                return n.round(2);
            else if (n.lessThan(1000000))
                return `${n.div(1000).round(2)}K`;
            
                return `${n.div(1000000).round(3)}M`;
        }

        private _who(transaction: Vidyano.QueryResultItem, property: string) {
            if (property === "Sender" || this._type(transaction) === "Out") {
                const who = transaction.values[property] as string;
                return who.startsWith("0x") && who.length > 20 ? `${who.slice(0, 8)}....${who.slice(who.length - 8, who.length)}` : who;
            }
            
            return "Bought flair"
        }

        private _when(transaction: Vidyano.QueryResultItem) {
            var date = new Date(transaction.values.Timestamp.toNumber() * 1000);
            return date.toLocaleString();
        }
    }
}