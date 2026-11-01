//
//  ContactListener.m
//  Shooter
//
//  Created by Giorgi Abelashvili on 11/2/10.
//  Copyright (c) 2010 __MyCompanyName__. All rights reserved.
//

#import "ContactListener.h"
#import "Figure.h"
#import "GameManager.h"
#import "MainCharacter.h"
#import "SoundManager.h"
#import "Bounder.h"
#import "Bullet.h"
#import "Weapon.h"
#import "EnemyAgent.h"
#import "BonusObject.h"
#import "BonusManager.h"
#import "GameModeManager.h"
#import "RushStation.h"
#import "Vitamin.h"

ContactListener::ContactListener()
{
}
//
ContactListener::~ContactListener()
{
}
//
void ContactListener::BeginContact(b2Contact* contact)
{
    //
    //get collided figure physics bodies
	b2Body *bodyA = contact->GetFixtureA()->GetBody();
    b2Body *bodyB = contact->GetFixtureB()->GetBody();
	//
    //get collided fugures
    Figure *f1 = (Figure*)bodyA->GetUserData();
    Figure *f2 = (Figure*)bodyB->GetUserData();
    //
    if(f1.type == kBullet && f2.type == kBullet)
    {
        //if([(Bullet*)f1 tag] == FRUNCTUS_BULLET_TAG)
            f2.flag = kDealloc;
        //else if([(Bullet*)f2 tag] == FRUNCTUS_BULLET_TAG)
            f1.flag = kDealloc;
    }
    //
    if(f1.type == kBounder && f2.type == kBullet)
    {
        f2.flag = kDealloc;
    }
    else if(f2.type == kBounder && f1.type == kBullet)
    {
        f1.flag = kDealloc;
    }
    //
    if(f1.type == kBullet && f2.type == kEnemy)
    {
        if(f2.flag!=kDying)
        {
            if([(Bullet*)f1 tag]!=SHOOTING_COROSIA_BULLET_TAG && [(Bullet*)f1 tag]!=FRUNCTUS_BULLET_TAG)
                f1.flag = kDealloc;
        }
        if([(Bullet*)f1 tag]!=SHOOTING_COROSIA_BULLET_TAG && [(Bullet*)f1 tag]!=FRUNCTUS_BULLET_TAG)
        {
            [(EnemyAgent*)f2 hit];
            if(f2.flag == kDealloc && [[GameModeManager sharedGameModeManager] currentGameModeType]==kCompaignMode)
                [[GameManager sharedGameManager] updateLScoreWithValue:((EnemyAgent*)f2).killScore];
        }
    }
    else if(f2.type == kBullet && f1.type == kEnemy)
    {
        if(f1.flag!=kDying)
        {
            if([(Bullet*)f2 tag]!=SHOOTING_COROSIA_BULLET_TAG && [(Bullet*)f2 tag]!=FRUNCTUS_BULLET_TAG)
                f2.flag = kDealloc;
        }
        if([(Bullet*)f2 tag]!=SHOOTING_COROSIA_BULLET_TAG && [(Bullet*)f2 tag]!=FRUNCTUS_BULLET_TAG)
        {
            [(EnemyAgent*)f1 hit];
            if(f1.flag == kDealloc && [[GameModeManager sharedGameModeManager] currentGameModeType]==kCompaignMode)
                [[GameManager sharedGameManager] updateLScoreWithValue:((EnemyAgent*)f1).killScore];
        }
    }
    //
    if(f1.type == kMainCharacter && f2.type == kEnemy)
    {
        MainCharacter *character = (MainCharacter*)f1;
        if(f2.flag!=kDying)
        {
            [character hit];
        }
    }
    //
    else if(f2.type == kMainCharacter && f1.type == kEnemy)
    {
        MainCharacter *character = (MainCharacter*)f2;
        if(f1.flag!=kDying)
        {
            [character hit];
        }
    }
    //
    if(f1.type == kMainCharacter && f2.type == kBullet)
    {
        MainCharacter *character = (MainCharacter*)f1;
        [character hit];
        f2.flag = kDealloc;
    }
    //
    else if(f2.type == kMainCharacter && f1.type == kBullet)
    {
        MainCharacter *character = (MainCharacter*)f2;
        [character hit];
        f1.flag = kDealloc;
    }
    //
    if(f1.type == kBonusObject && f2.type == kMainCharacter)
    {
        BonusObject *bonus = (BonusObject*)f1;
        MainCharacter *character = (MainCharacter*)f2;
        bonus.flag = kDealloc;
        // this is flag bonus
        if(bonus.bonusObjectType == kFlagBonus)
        {
            [[GameManager sharedGameManager] updateLScoreWithValue:FLAG_CONST_POINT];
            [[GameModeManager sharedGameModeManager] canGenerateBonus:YES];
        }
        else if(bonus.bonusObjectType == kPeaceFlagBonus)
        {
            [[GameManager sharedGameManager] updateLScoreWithValue:FLAG_CONST_POINT];
            [[GameModeManager sharedGameModeManager] gameModeSpecialAction];
        }
        // this is usable bonus
        else {
            [character getBonusWithType:bonus.bonusObjectType];
        }
    }
    //
    else if(f2.type == kBonusObject && f1.type == kMainCharacter)
    {
        BonusObject *bonus = (BonusObject*)f2;
        MainCharacter *character = (MainCharacter*)f1;
        bonus.flag = kDealloc;
        // this is flag bonus
        if(bonus.bonusObjectType == kFlagBonus)
        {
            [[GameManager sharedGameManager] updateLScoreWithValue:FLAG_CONST_POINT];
            [[GameModeManager sharedGameModeManager] canGenerateBonus:YES];
        }
        else if(bonus.bonusObjectType == kPeaceFlagBonus)
        {
            [[GameManager sharedGameManager] updateLScoreWithValue:FLAG_CONST_POINT];
            [[GameModeManager sharedGameModeManager] gameModeSpecialAction];
        }
        // this is usable bonus
        else {
            [character getBonusWithType:bonus.bonusObjectType];
        }
    }
    //
    if(f1.type == kMainCharacter && f2.type == kRushStation)
    {
        RushStation *rushStation = (RushStation*)f2;
        [rushStation setCharacterIsIn:YES];
    }
    //
    else if(f2.type == kMainCharacter && f1.type == kRushStation)
    {
        RushStation *rushStation = (RushStation*)f1;
        [rushStation setCharacterIsIn:YES];
    }
    //
    if(f1.type == kMainCharacter && f2.type == kVitamin)
    {
        Vitamin *vitamin = (Vitamin*)f2;
        vitamin.flag = kDealloc;
        [[GameManager sharedGameManager] updateVitaminsCount];
    }
    //
    else if(f2.type == kMainCharacter && f1.type == kVitamin)
    {
        Vitamin *vitamin = (Vitamin*)f1;
        vitamin.flag = kDealloc;
        [[GameManager sharedGameManager] updateVitaminsCount];
    }
    //
}
//
void ContactListener::EndContact(b2Contact* contact)
{
    //get collided figure physics bodies
	b2Body *bodyA = contact->GetFixtureA()->GetBody();
    b2Body *bodyB = contact->GetFixtureB()->GetBody();
	//
    //get collided fugures
    Figure *f1 = (Figure*)bodyA->GetUserData();
    Figure *f2 = (Figure*)bodyB->GetUserData();
    //
    if(f1.type == kMainCharacter && f2.type == kRushStation)
    {
        RushStation *rushStation = (RushStation*)f2;
        [rushStation setCharacterIsIn:NO];
    }
    //
    else if(f2.type == kMainCharacter && f1.type == kRushStation)
    {
        RushStation *rushStation = (RushStation*)f1;
        [rushStation setCharacterIsIn:NO];
    }
    //
}

void ContactListener::PreSolve(b2Contact* contact, const b2Manifold* oldManifold)
{
}

void ContactListener::PostSolve(const b2Contact* contact, const b2ContactImpulse* impulse)
{
}