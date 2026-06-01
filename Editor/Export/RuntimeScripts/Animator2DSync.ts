const { regClass } = Laya;

/**
 * Animator2DSync — AnimatorStateScript，同步 3D Animator 与子节点 UI3D 中的 Animator2D
 *
 * 挂载在 AnimatorController 的每个 AnimatorState 上。
 * 当 3D Animator 进入某个状态时，自动发现子 UI3D 内的所有 Animator2D，
 * 并播放同名状态，保持速度一致。
 */
@regClass()
export class Animator2DSync extends Laya.AnimatorStateScript {

    private _animator2Ds: Laya.Animator2D[] | null = null;

    /**
     * 状态进入时：播放同名状态到所有 Animator2D
     */
    onStateEnter(): void {
        const animator = this.playStateInfo.animator;
        const stateName = this.playStateInfo.playState.name;

        if (!this._animator2Ds) {
            this._collectAnimator2Ds(animator.owner as Laya.Sprite3D);
        }

        if (this._animator2Ds.length === 0) return;

        for (let i = 0; i < this._animator2Ds.length; i++) {
            this._animator2Ds[i].play(stateName, 0, 0);
            this._animator2Ds[i].speed = animator.speed;
        }
    }

    /**
     * 每帧更新：保持速度同步，处理 UI3D prefab 延迟加载
     */
    onStateUpdate(): void {
        const animator = this.playStateInfo.animator;

        // Lazy collect: UI3D prefab may load async, retry until found
        if (!this._animator2Ds || this._animator2Ds.length === 0) {
            this._animator2Ds = null;
            this._collectAnimator2Ds(animator.owner as Laya.Sprite3D);
            if (this._animator2Ds.length === 0) {
                this._animator2Ds = null;
                return;
            }
            // Late init: play current state
            const stateName = this.playStateInfo.playState.name;
            for (let i = 0; i < this._animator2Ds.length; i++) {
                this._animator2Ds[i].play(stateName, 0, 0);
            }
        }

        // Sync speed
        const speed = animator.speed;
        for (let i = 0; i < this._animator2Ds.length; i++) {
            this._animator2Ds[i].speed = speed;
        }
    }

    private _collectAnimator2Ds(node: Laya.Sprite3D): void {
        this._animator2Ds = [];
        this._findUI3DRecursive(node);
    }

    private _findUI3DRecursive(node: Laya.Sprite3D): void {
        for (let i = 0, n = node.numChildren; i < n; i++) {
            const child = node.getChildAt(i) as Laya.Sprite3D;
            const ui3d = child.getComponent(Laya.UI3D);
            if (ui3d && ui3d.sprite) {
                this._findAnimator2DInSprite(ui3d.sprite);
            }
            this._findUI3DRecursive(child);
        }
    }

    private _findAnimator2DInSprite(node: Laya.Sprite): void {
        const a2d = node.getComponent(Laya.Animator2D);
        if (a2d) {
            this._animator2Ds!.push(a2d);
            return;
        }
        for (let i = 0, n = node.numChildren; i < n; i++) {
            this._findAnimator2DInSprite(node.getChildAt(i) as Laya.Sprite);
        }
    }
}
