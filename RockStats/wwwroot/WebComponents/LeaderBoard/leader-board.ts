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
    type Sort = "balance" | "sent";

    @Vidyano.WebComponents.WebComponent.register({
        properties: {
            accounts: {
                type: Object,
                readOnly: true
            },
            sort: {
                type: String,
                readOnly: true,
                value: "balance",
                observer: "_sortChanged"
            },
            isBusy: {
                type: Boolean,
                readOnly: true,
                value: true
            }
        },
        forwardObservers: [
            "accounts.items"
        ],
        mediaQueryAttributes: true
    }, "rockstats")
    export class LeaderBoard extends Vidyano.WebComponents.WebComponent {
        readonly accounts: Vidyano.Query; private _setAccounts: (accounts: Vidyano.Query) => void;
        readonly sort: string; private _setSort: (sort: string) => void;
        readonly isBusy: boolean; private _setIsBusy: (isBusy: boolean) => void;

        async attached() {
            super.attached();

            await this.app.initialize;
            this._setAccounts(await this.app.service.getQuery("2739d6b8-5c14-4c27-9f04-19bf6cdbaf5a"));

            const list = <any>Polymer.dom(this.root).querySelector("#list");
            list.scrollTarget = this.findParent(e => e instanceof Vidyano.WebComponents.Scroller, list)["scroller"];
            list.fire("iron-resize");

            this._setIsBusy(false);
        }

        private _items(items: Vidyano.QueryResultItem[]) {
            const column = this.sort === "balance" ? "Balance" : "Sent";
            const result = items.filter(i => i.values.Redditor != null && i.values.Avatar != null && (i.values[column] as BigNumber).greaterThan(0));
            
            let index = 0;
            let previous: BigNumber = null;

            result.forEach(i => {
                const value = i.values[column] as BigNumber;
                if (previous == null || value.lessThan(previous)) {
                    index++;
                    previous = value;
                }
                
                i.id = <any>index;
            });

            return result;
        }

        private _address(account: Vidyano.QueryResultItem) {
            return `https://blockexplorer.mainnet.v1.omg.network/address/${account.values.Address}?section=transactions`;
        }

        private _flairs(flairs: string) {
            if (flairs == null)
                return [];

            return flairs.split(":").filter(f => f != null && f.length > 0).map(f => {
                switch (f) {
                    case "rock":
                        return "https://emoji.redditmedia.com/l9rufetdlxd51_t5_m3idt/rock";
                    
                    case "salamander":
                        return "https://emoji.redditmedia.com/b517pvmtlxd51_t5_m3idt/salamander";

                    case "soon":
                        return "https://emoji.redditmedia.com/g4dm01b3mxd51_t5_m3idt/soon";
                    
                    case "omg":
                        return "https://emoji.redditmedia.com/htvyq0opp6d51_t5_m3idt/omg";

                    case "wve":
                        return "https://emoji.redditmedia.com/2pl9xkmspnk51_t5_m3idt/wve";

                    case "snl":
                        return "https://emoji.redditmedia.com/88nfcm4xpnk51_t5_m3idt/snl";

                    case "skb":
                        return "https://emoji.redditmedia.com/at87t5aopnk51_t5_m3idt/skb";

                    default:
                        return null;
                }
            }).filter(f => !!f);
        }

        private _avatar(account: Vidyano.QueryResultItem) {
            return account.values.Avatar?.split("?")[0];
        }

        private _balance(account: Vidyano.QueryResultItem, sort: Sort) {
            const n: BigNumber = account.values[sort === "balance" ? "Balance" : "Sent"];

            if (n.lessThan(10000))
                return n.round(2);
            else if (n.lessThan(1000000))
                return `${n.div(1000).round(2)}K`;
            
                return `${n.div(1000000).round(3)}M`;
        }

        private _isSortActive(sort: Sort, term: Sort) {
            return sort === term;
        }

        private _sort(e: TapEvent) {
            if (this.isBusy)
                return;

            const sort = (e.target as HTMLElement).getAttribute("sort");
            if (sort == null)
                return;

            this._setSort(sort);
        }

        private async _sortChanged(sort: Sort) {
            if (!this.accounts || this.isBusy)
                return;

            const columnName = sort === "balance" ? "Balance" : "Sent";
            this.accounts.getColumn(columnName).sort(Vidyano.SortDirection.Descending);

            try {
                this._setIsBusy(true);
                await this.accounts.search();
            }
            finally {
                this._setIsBusy(false);
            }
        }

        private _showMedal(index: number) {
            return index <= 3;
        }
    }
}