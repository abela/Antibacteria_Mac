//
//  SettingsLayer.h
//  AntiBacteria
//
//  Created by Giorgi Abelashvili on 5/9/13.
//
//

#import "GameMenuLayer.h"
#import "cocos2d.h"

@interface SettingsLayer : GameMenuLayer
{
    CCSprite *difficultyLeft;
    CCSprite *difficultyRight;
    //
    CCSprite *controlTypeLeft;
    CCSprite *controlTypeRight;
    //
    CCSprite *autoFireIsOn;
    CCSprite *sfxIsEnabledSprite;
    CCSprite *soundIsEnabledSprite;
    //
    //
    CCLabelTTF *languageTitleLabel;
    CCLabelTTF *languageLabel;
    CCLabelTTF *titleLabel;
    CCLabelTTF *difficultyLabel;
    CCLabelTTF *autoShootLabel;
    CCLabelTTF *controlTypeLabel;
    CCLabelTTF *sfxEnabledLabel;
    CCLabelTTF *soundIsEnabled;
    CCSprite   *languageLeft;
    CCSprite   *languageRight;
    //
    CCLabelTTF *difficultyValueLabel;
    CCLabelTTF *controlLabel;
    //
    int gameDifficulty;
    int controlType;
    BOOL autoFireIsOnValue;
    //
}
@end
