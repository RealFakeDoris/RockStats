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
    type Sort = "balance" | "sent" | "in" | "out";
    export const flairs_map = {
        "rock": "https://emoji.redditmedia.com/l9rufetdlxd51_t5_m3idt/rock",
        "salamander": "https://emoji.redditmedia.com/b517pvmtlxd51_t5_m3idt/salamander",
        "soon": "https://emoji.redditmedia.com/g4dm01b3mxd51_t5_m3idt/soon",        
        "omg": "https://emoji.redditmedia.com/htvyq0opp6d51_t5_m3idt/omg",
        "wve": "https://emoji.redditmedia.com/2pl9xkmspnk51_t5_m3idt/wve",
        "snl": "https://emoji.redditmedia.com/88nfcm4xpnk51_t5_m3idt/snl",
        "skb":"https://emoji.redditmedia.com/at87t5aopnk51_t5_m3idt/skb"
    };

    @Vidyano.WebComponents.WebComponent.register({
        properties: {
            accounts: {
                type: Object,
                readOnly: true
            },
            items: {
                type: Array,
                computed: "_items(accounts.items, sort)"
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
            },
            selectedAccount: {
                type: Object,
                value: null
            },
            hasSelectedAccount: {
                type: Boolean,
                computed: "_computeHasSelectedAccount(selectedAccount)",
                reflectToAttribute: true
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
        selectedAccount: Vidyano.QueryResultItem;

        async attached() {
            super.attached();

            await this.app.initialize;
            this._setAccounts(await this.app.service.getQuery("2739d6b8-5c14-4c27-9f04-19bf6cdbaf5a"));

            const list = <any>Polymer.dom(this.root).querySelector("#list");
            list.scrollTarget = this.findParent(e => e instanceof Vidyano.WebComponents.Scroller, list)["scroller"];
            list.fire("iron-resize");

            this._setIsBusy(false);
        }

        private _items(items: Vidyano.QueryResultItem[], sort: string) {
            if (items == null)
                return [];

            if (!document.baseURI.trimEnd('/').endsWith("/team"))
                items = items.filter(i => !i.values.Moderator);
            else
                items = items.filter(i => i.values.Moderator);

            let index = 0;
            let result: Vidyano.QueryResultItem[];
            if (sort == "balance" || sort == "sent") {
                const column = sort === "balance" ? "Balance" : "Sent";
                const columnBN = column + "BigNumber";
                const div = new BigNumber("1000000000000000000");
                result = items.filter(i => i.values.Redditor != null && i.values.Avatar != null).map(i => {
                    if (!i.values[columnBN])
                        i.values[columnBN] = (new BigNumber(i.values[column])).div(div);

                    return i;
                }).filter(i => (i.values[columnBN] as BigNumber).greaterThan(0)).sort((a, b) => {
                    return a.values[columnBN].greaterThan(b.values[columnBN]) ? -1 : 1;
                });
                
                let previous: BigNumber = null;

                result.forEach(i => {
                    const value = i.values[columnBN] as BigNumber;
                    if (previous == null || value.lessThan(previous)) {
                        index++;
                        previous = value;
                    }
                    
                    i.values.Rank = <any>index;
                });
            } else {
                let column: string;
                switch(sort) {
                    case "in": {
                        column = "TxsIn";
                        break;
                    }
                    case "out": {
                        column = "TxsOut";
                        break;
                    }
                }

                let previous;
                result = items.filter(i => i.values[column] > 0).map(i => {
                    const value = i.values[column] as number;
                    if (previous == null || value < previous) {
                        index++;
                        previous = value;
                    }
                    
                    i.values.Rank = <any>index;

                    return i;
                });
            }

            return result;
        }
        
        private _select(e: TapEvent) {
            this.selectedAccount = e.model.account;
        }

        private _closeSelectedAccount() {
            this.selectedAccount = null;
        }

        _computeHasSelectedAccount(selectedAccount: Vidyano.QueryResultItem) {
            return selectedAccount != null;
        }

        private _address(account: Vidyano.QueryResultItem) {
            return `https://blockexplorer.mainnet.v1.omg.network/address/${account.values.Address}?section=transactions`;
        }

        private _flairs(flairs: string) {
            if (flairs == null)
                return [];

            return flairs.split(":").filter(f => f != null && f.length > 0).map(f => flairs_map[f] ?? null).filter(f => !!f);
        }

        private _avatar(account: Vidyano.QueryResultItem) {
            return account.values.Avatar?.split("?")[0];
        }

        private _value(account: Vidyano.QueryResultItem, sort: Sort) {
            if (sort !== "balance" && sort !== "sent") {
                if (sort == "in")
                    return account.values.TxsIn;
                else if (sort == "out")
                    return account.values.TxsOut;
            }

            const n: BigNumber = account.values[(sort === "balance" ? "Balance" : "Sent") + "BigNumber"];

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

            let columnName: string;
            switch(sort) {
                case "balance": {
                    columnName = "Balance"
                    break;
                }
                case "sent": {
                    columnName = "Sent"
                    break;
                }
                case "in": {
                    columnName = "TxsIn"
                    break;
                }
                case "out": {
                    columnName = "TxsOut"
                    break;
                }
            }
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