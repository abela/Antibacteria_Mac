//
//  ChooseLevelLayer.h
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/26/13.
//
//

#import "GameMenuLayer.h"
#include <vector>
using namespace std;

@interface ChooseLevelLayer : GameMenuLayer
{
    CCMenu *chooseGameModeMenu;
    int currentLevel;
    int gameMode;
    vector <CCSprite*> gameModesSprites;
}
@end
