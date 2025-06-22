//
//  CharacterUpgradeLayer.h
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 6/22/13.
//
//

#import "cocos2d.h"
#import "GameMenuLayer.h"

@interface CharacterUpgradeLayer : GameMenuLayer
{
    NSMutableArray *buyButtons;
    NSMutableArray *arrawyWithExchangeValues;
    CCLabelTTF *vitaminsCountLabel;
    NSArray *inAppKeysArray;
    NSArray *inAppValuesArray;
    NSArray *encriptedValuesArray;
    NSMutableArray *priceLabelsArray;
}
@end
