"use strict";
var RockStats;
(function (RockStats) {
    var WebComponents;
    (function (WebComponents) {
        WebComponents.Dead = "0x000000000000000000000000000000000000dead";
        var Transactions = (function (_super) {
            __extends(Transactions, _super);
            function Transactions() {
                return _super !== null && _super.apply(this, arguments) || this;
            }
            Transactions.prototype.attached = function () {
                return __awaiter(this, void 0, void 0, function () {
                    var _a, items, list;
                    return __generator(this, function (_b) {
                        switch (_b.label) {
                            case 0:
                                _super.prototype.attached.call(this);
                                return [4, this.app.initialize];
                            case 1:
                                _b.sent();
                                _a = this._setTransactions;
                                return [4, this.app.service.getQuery("e39930f9-b109-44e9-bfc9-a1a59d94ed5c")];
                            case 2:
                                _a.apply(this, [_b.sent()]);
                                items = this.transactions.items;
                                if (items.length < this.transactions.totalItems)
                                    this._applyLoadMore(items);
                                this._setItems(items);
                                list = Polymer.dom(this.root).querySelector("#list");
                                list.scrollTarget = this.findParent(function (e) { return e instanceof Vidyano.WebComponents.Scroller; }, list)["scroller"];
                                list.fire("iron-resize");
                                this._setIsBusy(false);
                                return [2];
                        }
                    });
                });
            };
            Transactions.prototype._sizeChanged = function () {
                Polymer.dom(this.root).querySelector("#list").fire("iron-resize");
            };
            Transactions.prototype._applyLoadMore = function (items) {
                var _this = this;
                items[items.length - 1] = new Proxy(items[items.length - 1], {
                    get: function (target, prop, receiver) {
                        if (prop === "loadMore") {
                            _this.set("items." + (items.length - 1), target);
                            var txs = _this.transactions.clone();
                            txs.skip = items.length;
                            txs.search().then(function (newItems) {
                                var currentLength = items.length;
                                [].splice.apply(items, [items.length, 0].concat(newItems));
                                if ((items.length + newItems.length) < _this.transactions.totalItems)
                                    _this._applyLoadMore(items);
                                _this.notifySplices("items", [
                                    { index: currentLength, removed: [], addedCount: newItems.length, object: items, type: 'splice' }
                                ]);
                            });
                            return;
                        }
                        return target[prop];
                    }
                });
            };
            Transactions.prototype._transactionHref = function (transaction) {
                return "https://blockexplorer.mainnet.v1.omg.network/transaction/" + transaction.values.Hash;
            };
            Transactions.prototype._type = function (tx) {
                if (tx.values.Receiver === WebComponents.Dead && WebComponents.metadata_map[tx.values.Metadata] != null)
                    return "Flair";
                return "Out";
            };
            Transactions.prototype._avatar = function (transaction, property) {
                var _a;
                if (this._type(transaction) === "Flair" && property === "ReceiverAvatar")
                    return WebComponents.flairs_map[WebComponents.metadata_map[transaction.values.Metadata]];
                return ((_a = transaction.values[property]) === null || _a === void 0 ? void 0 : _a.split("?")[0]) || "";
            };
            Transactions.prototype._amount = function (transaction) {
                var amount = transaction.values.AmountBigNumber;
                if (amount == null) {
                    var div = new BigNumber("1000000000000000000");
                    transaction.values.AmountBigNumber = (new BigNumber(transaction.values.Amount)).div(div);
                }
                var n = transaction.values.AmountBigNumber;
                if (n.lessThan(new BigNumber("0.0000001")))
                    return "< 0.0000001";
                if (n.lessThan(10000))
                    return n.round(2);
                else if (n.lessThan(1000000))
                    return n.div(1000).round(2) + "K";
                return n.div(1000000).round(3) + "M";
            };
            Transactions.prototype._who = function (transaction, property) {
                if (property === "Sender" || this._type(transaction) === "Out") {
                    var who = transaction.values[property];
                    return who.startsWith("0x") && who.length > 20 ? who.slice(0, 8) + "...." + who.slice(who.length - 8, who.length) : who;
                }
                return "Bought flair";
            };
            Transactions.prototype._when = function (transaction) {
                var date = new Date(transaction.values.Timestamp.toNumber() * 1000);
                return date.toLocaleString();
            };
            Transactions = __decorate([
                Vidyano.WebComponents.WebComponent.register({
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
            ], Transactions);
            return Transactions;
        }(Vidyano.WebComponents.WebComponent));
        WebComponents.Transactions = Transactions;
    })(WebComponents = RockStats.WebComponents || (RockStats.WebComponents = {}));
})(RockStats || (RockStats = {}));
