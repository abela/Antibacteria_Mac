//
//  BonusManager.h
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/30/13.
//
//

#import <Foundation/Foundation.h>
#include <vector>
using namespace std;
//
@class MainGameLayer;
@class BonusObject;
//
@interface BonusManager : NSObject
{
    MainGameLayer *gameLayer;
    vector <BonusObject*> bonusObjects;
}
@property (nonatomic,assign) MainGameLayer *gameLayer;
@property (readonly) vector <BonusObject*> bonusObjects;
+(BonusManager*) sharedBonusManager;
-(void) generateRandomBonus;
-(void) generateLifeBonus;
-(void) generateFlag;
-(void) generateRandomPeaceWalkerBonus;
-(void) generateRushStation;
-(void) update;
-(void) releaseBonusManager;
-(void) generateBonusWithType:(BonusObjectType)bonusObjectType;
@end
