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
    export const metadata_map = {
        "0x00000000000000000000000000000000000000000000666c6169725f726f636b": "rock",
        "0x0000000000000000000000000000000000666c6169725f736f6f6e616c697361": "snl",
        "0x00000000000000000000000000000000666c6169725f73616c616d616e646572": "salamander",
        "0x00000000000000000000000000000000000000000000666c6169725f736f6f6e": "soon",
        "0x00000000000000000000000000000000000000000000666c6169725f77617665": "wve",
        "0x000000000000000000000000000000000000000000666c6169725f736b617465": "skb"
    };

    @Vidyano.WebComponents.WebComponent.register({
        properties: {
            accountItem: {
                type: Object,
                notify: true,
                observer: "_accountItemChanged"
            },
            account: {
                type: Object,
                readOnly: true
            },
            loading: {
                type: Boolean,
                readOnly: true
            }
        }
    }, "rockstats")
    export class AccountDetails extends Vidyano.WebComponents.WebComponent {
        readonly loading: boolean; private _setLoading: (loading: boolean) => void;
        readonly account: Vidyano.PersistentObject; private _setAccount: (account: Vidyano.PersistentObject) => void;
        accountItem: Vidyano.QueryResultItem;

        attached() {
            super.attached();

            const list = <any>Polymer.dom(this.root).querySelector("#list");
            list.scrollTarget = this.findParent(e => e instanceof Vidyano.WebComponents.Scroller, list)["scroller"];
            list.fire("iron-resize");
        }

        private _close() {
            this.accountItem = null;
        }

        private async _accountItemChanged(accountItem: Vidyano.QueryResultItem, oldAccountItem: Vidyano.QueryResultItem) {
            if (accountItem == null) {
                if (oldAccountItem != null)
                    await this.sleep(300);

                this._setAccount(null);
                return;
            }

            this._setLoading(true);
            try {
                const result = await accountItem.getPersistentObject(true);
                
                await this.sleep(300);
                this._setAccount(result);
            }
            catch(e) {
                this.app.showAlert(e, Vidyano.NotificationType.Error, 3000);
            }
            finally {
                this._setLoading(false);
            }
        }

        private _avatar(account: Vidyano.QueryResultItem) {
            if (account == null)
                return "";

            return account.values.Avatar?.split("?")[0];
        }

        private _label(transaction: Vidyano.QueryResultItem){
            if (this._isType(transaction, "flair"))
                return `Bought flair`;

            if (transaction.values.Sender === this.account.getAttributeValue("Owner"))
                return transaction.values.Receiver;
            
            return transaction.values.Sender;
        }

        private _flair(transaction: Vidyano.QueryResultItem) {
            const flair = metadata_map[transaction.values.Metadata];
            if (flair == null)
                return "";
            
            return flairs_map[flair];
        }

        private _type(transaction: Vidyano.QueryResultItem) {
            if (transaction.values.Receiver === Dead) {
                if (metadata_map[transaction.values.Metadata] != null)
                    return "Flair";
                else
                    return "Out";
            }
            else if (transaction.values.Receiver !== this.account.getAttributeValue("Owner"))
                return "Out";
            else if(transaction.values.Receiver === this.account.getAttributeValue("Owner"))
                return "In";
        }

        private _isType(transaction: Vidyano.QueryResultItem, type: string) {
            if (transaction.values.Receiver === Dead) {
                if (type === "flair")
                    return true;
                else
                    return false;
            }
            else if (transaction.values.Receiver !== this.account.getAttributeValue("Owner") && type === "out")
                return true;
            else if(transaction.values.Receiver === this.account.getAttributeValue("Owner") && type === "in")
                return true;

            return false;
        }

        private _amount(transaction: Vidyano.QueryResultItem) {
            const amount = transaction.values.Amount;
            let result = new BigNumber(amount).div(new BigNumber("1000000000000000000"));
            if (!this._isType(transaction, "in"))
                result = result.mul(-1);

            return result.toFormat()
        }

        private _amountType(transaction: Vidyano.QueryResultItem) {
            return this._isType(transaction, "in") ? "in" : "out";
        }

        private _transactionHref(transaction: Vidyano.QueryResultItem) {
            return `https://blockexplorer.mainnet.v1.omg.network/transaction/${transaction.values.Hash}`;
        }

        private _when(transaction: Vidyano.QueryResultItem) {
            var date = new Date(transaction.values.Timestamp.toNumber() * 1000);
            return date.toLocaleString();
        }
    }
}