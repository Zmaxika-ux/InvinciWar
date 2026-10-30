# RivalsLike – FPS 3D de duels (Unity / C#)

Prototype de FPS compétitif en duel 1v1 contre un bot, dans le style des jeux de duels rapides façon *Rivals* :
manches courtes, armes variées, mouvements nerveux. Tout le contenu (arène, armes, bot) est original.

## Installation

1. Créer un projet Unity **3D** (Unity 2022.3 LTS ou Unity 6).
2. Copier le dossier `Assets/Scripts` de ce dépôt dans le dossier `Assets` du projet.
3. **Important** : `Edit > Project Settings > Player > Other Settings > Active Input Handling` → choisir
   **Input Manager (Old)** ou **Both** (le jeu utilise l'ancien système d'entrées), puis laisser Unity redémarrer.
4. Ouvrir une scène (la scène vide par défaut suffit) et appuyer sur **Play**.

`GameBootstrap` construit automatiquement l'arène, le joueur, le bot, la logique de manches et le HUD au lancement.
Aucune configuration de scène ni de prefab n'est nécessaire.

## Contrôles

| Action | Touche |
|---|---|
| Se déplacer | WASD / flèches |
| Regarder | Souris |
| Sauter (double saut) | Espace |
| Sprint | Maj gauche |
| Dash | E |
| Tirer | Clic gauche |
| Zoom (sniper) | Clic droit |
| Recharger | R |
| Changer d'arme | 1-4 ou molette |
| Difficulté du bot | F1 Facile · F2 Normal · F3 Difficile |
| Libérer / reprendre la souris | Échap / clic |

## Règles

- Premier à **5 manches** gagne (modifiable dans `MatchManager`).
- Chaque manche dure 90 s ; au timeout, celui qui a le plus de PV marque le point.
- Les headshots font ×2 de dégâts.
- Le bot change d'arme à chaque manche.

## Armes

| Arme | Dégâts | Cadence | Particularité |
|---|---|---|---|
| Pistolet | 24 | 6/s | précis, semi-auto |
| Fusil d'assaut | 13 | 11/s | automatique |
| Fusil à pompe | 8 × 11 | 1,1/s | courte portée |
| Sniper | 85 | 1/s | zoom, gros recul |

## Structure

```
Assets/Scripts/
├── GameBootstrap.cs     Construit arène, joueur, bot, HUD au lancement
├── MatchManager.cs      Manches, score, compte à rebours, difficulté
├── PlayerController.cs  Vue, déplacements, saut, dash, entrées armes
├── WeaponController.cs  Armes, munitions, rechargement, tir hitscan, effets
├── BotAI.cs             IA : poursuite, esquive, visée avec erreur, tir
├── Health.cs            Points de vie, événements dégâts / mort
└── HUD.cs               Viseur, hitmarker, vie, munitions, score (OnGUI)
```

## Idées pour la suite

- Multijoueur (Netcode for GameObjects / Mirror / Photon Fusion)
- Vrais modèles 3D, animations, sons
- Navigation du bot avec NavMesh
- Menu principal, sélection de loadout, plusieurs cartes
- Système de classement et de progression
