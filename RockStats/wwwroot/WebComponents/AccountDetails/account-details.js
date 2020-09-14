"use strict";
var RockStats;
(function (RockStats) {
    var WebComponents;
    (function (WebComponents) {
        WebComponents.metadata_map = {
            "0x00000000000000000000000000000000000000000000666c6169725f726f636b": "rock",
            "0x0000000000000000000000000000000000666c6169725f736f6f6e616c697361": "snl",
            "0x00000000000000000000000000000000666c6169725f73616c616d616e646572": "salamander",
            "0x00000000000000000000000000000000000000000000666c6169725f736f6f6e": "soon",
            "0x00000000000000000000000000000000000000000000666c6169725f77617665": "wve",
            "0x000000000000000000000000000000000000000000666c6169725f736b617465": "skb"
        };
        var AccountDetails = (function (_super) {
            __extends(AccountDetails, _super);
            function AccountDetails() {
                return _super !== null && _super.apply(this, arguments) || this;
            }
            AccountDetails.prototype.attached = function () {
                _super.prototype.attached.call(this);
                var list = Polymer.dom(this.root).querySelector("#list");
                list.scrollTarget = this.findParent(function (e) { return e instanceof Vidyano.WebComponents.Scroller; }, list)["scroller"];
                list.fire("iron-resize");
            };
            AccountDetails.prototype._close = function () {
                this.accountItem = null;
            };
            AccountDetails.prototype._accountItemChanged = function (accountItem, oldAccountItem) {
                return __awaiter(this, void 0, void 0, function () {
                    var result, e_1;
                    return __generator(this, function (_a) {
                        switch (_a.label) {
                            case 0:
                                if (!(accountItem == null)) return [3, 3];
                                if (!(oldAccountItem != null)) return [3, 2];
                                return [4, this.sleep(300)];
                            case 1:
                                _a.sent();
                                _a.label = 2;
                            case 2:
                                this._setAccount(null);
                                return [2];
                            case 3:
                                this._setLoading(true);
                                _a.label = 4;
                            case 4:
                                _a.trys.push([4, 7, 8, 9]);
                                return [4, accountItem.getPersistentObject(true)];
                            case 5:
                                result = _a.sent();
                                return [4, this.sleep(300)];
                            case 6:
                                _a.sent();
                                this._setAccount(result);
                                return [3, 9];
                            case 7:
                                e_1 = _a.sent();
                                this.app.showAlert(e_1, Vidyano.NotificationType.Error, 3000);
                                return [3, 9];
                            case 8:
                                this._setLoading(false);
                                return [7];
                            case 9: return [2];
                        }
                    });
                });
            };
            AccountDetails.prototype._avatar = function (account) {
                var _a;
                if (account == null)
                    return "";
                return (_a = account.values.Avatar) === null || _a === void 0 ? void 0 : _a.split("?")[0];
            };
            AccountDetails.prototype._label = function (transaction) {
                if (this._isType(transaction, "flair"))
                    return "Bought flair";
                if (transaction.values.Sender === this.account.getAttributeValue("Owner"))
                    return transaction.values.Receiver;
                return transaction.values.Sender;
            };
            AccountDetails.prototype._flair = function (transaction) {
                var flair = WebComponents.metadata_map[transaction.values.Metadata];
                if (flair == null)
                    return "";
                return WebComponents.flairs_map[flair];
            };
            AccountDetails.prototype._type = function (transaction) {
                if (transaction.values.Receiver === WebComponents.Dead) {
                    if (WebComponents.metadata_map[transaction.values.Metadata] != null)
                        return "Flair";
                    else
                        return "Out";
                }
                else if (transaction.values.Receiver !== this.account.getAttributeValue("Owner"))
                    return "Out";
                else if (transaction.values.Receiver === this.account.getAttributeValue("Owner"))
                    return "In";
            };
            AccountDetails.prototype._isType = function (transaction, type) {
                if (transaction.values.Receiver === WebComponents.Dead) {
                    if (type === "flair")
                        return true;
                    else
                        return false;
                }
                else if (transaction.values.Receiver !== this.account.getAttributeValue("Owner") && type === "out")
                    return true;
                else if (transaction.values.Receiver === this.account.getAttributeValue("Owner") && type === "in")
                    return true;
                return false;
            };
            AccountDetails.prototype._amount = function (transaction) {
                var amount = transaction.values.Amount;
                var result = new BigNumber(amount).div(new BigNumber("1000000000000000000"));
                if (!this._isType(transaction, "in"))
                    result = result.mul(-1);
                return result.toFormat();
            };
            AccountDetails.prototype._amountType = function (transaction) {
                return this._isType(transaction, "in") ? "in" : "out";
            };
            AccountDetails.prototype._transactionHref = function (transaction) {
                return "https://blockexplorer.mainnet.v1.omg.network/transaction/" + transaction.values.Hash;
            };
            AccountDetails.prototype._when = function (transaction) {
                var date = new Date(transaction.values.Timestamp.toNumber() * 1000);
                return date.toLocaleString();
            };
            AccountDetails = __decorate([
                Vidyano.WebComponents.WebComponent.register({
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
            ], AccountDetails);
            return AccountDetails;
        }(Vidyano.WebComponents.WebComponent));
        WebComponents.AccountDetails = AccountDetails;
    })(WebComponents = RockStats.WebComponents || (RockStats.WebComponents = {}));
})(RockStats || (RockStats = {}));
