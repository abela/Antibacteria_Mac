//
//  GameSettingsManager.h
//  Turtle
//
//  Created by Giorgi Abelashvili on 5/22/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>


@interface GameSettingsManager : NSObject {
	@private
    int vitaminsCount;
	BOOL soundIsEnabled;
	BOOL sfxIsEnabled;
    BOOL isAutoFireEnabled;
    GameDifficulty gameDifficulty;
    CharacterMoveType controlType;
    int openedGameModes;
}
//
@property (nonatomic,assign) int vitaminsCount;
@property (nonatomic,assign) BOOL soundIsEnabled;
@property (nonatomic,assign) BOOL sfxIsEnabled;
@property (nonatomic,assign) GameDifficulty gameDifficulty;
@property (nonatomic,assign) int openedGameModes;
@property (nonatomic,assign) CharacterMoveType controlType;
@property (nonatomic,assign) BOOL isAutoFireEnabled;
//
+(GameSettingsManager*)sharedGameSettingsManager;
//
-(void) loadGameSettings;
//
-(void) saveGameSettings;
//
@end
